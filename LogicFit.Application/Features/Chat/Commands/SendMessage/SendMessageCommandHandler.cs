using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Chat.Commands.SendMessage;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public SendMessageCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var currentUserId = Guid.Parse(_currentUserService.UserId!);

        ChatConversation? conversation;

        if (request.ConversationId.HasValue)
        {
            // Use existing conversation
            conversation = await _context.ChatConversations
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value
                    && c.TenantId == tenantId
                    && !c.IsDeleted, cancellationToken);

            if (conversation == null)
                throw new NotFoundException("ChatConversation", request.ConversationId.Value);
        }
        else
        {
            if (!request.RecipientId.HasValue)
                throw new InvalidOperationException("Either ConversationId or RecipientId must be provided.");

            var recipientId = request.RecipientId.Value;

            // Determine if current user is coach or client
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == currentUserId && u.TenantId == tenantId, cancellationToken);

            if (currentUser == null)
                throw new NotFoundException("User", currentUserId);

            Guid coachId, clientId;

            if (currentUser.Role == UserRole.Coach || currentUser.Role == UserRole.Owner)
            {
                coachId = currentUserId;
                clientId = recipientId;
            }
            else
            {
                coachId = recipientId;
                clientId = currentUserId;
            }

            // Find existing conversation
            conversation = await _context.ChatConversations
                .FirstOrDefaultAsync(c => c.CoachId == coachId
                    && c.ClientId == clientId
                    && c.TenantId == tenantId
                    && !c.IsDeleted, cancellationToken);

            // Create new conversation if none exists
            if (conversation == null)
            {
                conversation = new ChatConversation
                {
                    TenantId = tenantId,
                    CoachId = coachId,
                    ClientId = clientId,
                    LastMessageAt = DateTime.UtcNow
                };

                _context.ChatConversations.Add(conversation);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        // Create the message
        var message = new ChatMessage
        {
            TenantId = tenantId,
            ConversationId = conversation.Id,
            SenderId = currentUserId,
            Content = request.Content,
            IsRead = false
        };

        _context.ChatMessages.Add(message);

        // Update conversation last message time
        conversation.LastMessageAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return message.Id;
    }
}

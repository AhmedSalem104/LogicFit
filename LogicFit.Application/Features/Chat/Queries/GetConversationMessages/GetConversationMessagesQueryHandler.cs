using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Chat.DTOs;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Chat.Queries.GetConversationMessages;

public class GetConversationMessagesQueryHandler : IRequestHandler<GetConversationMessagesQuery, List<ChatMessageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetConversationMessagesQueryHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<List<ChatMessageDto>> Handle(GetConversationMessagesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var userId = Guid.Parse(_currentUserService.UserId!);

        var conversation = await _context.ChatConversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId
                && c.TenantId == tenantId
                && !c.IsDeleted, cancellationToken);

        if (conversation == null)
            throw new NotFoundException("ChatConversation", request.ConversationId);

        // Only the two participants may read the conversation.
        if (conversation.CoachId != userId && conversation.ClientId != userId)
            throw new ForbiddenException("You are not a participant in this conversation");

        var messages = await _context.ChatMessages
            .Include(m => m.Sender).ThenInclude(s => s.Profile)
            .Where(m => m.ConversationId == request.ConversationId
                && m.TenantId == tenantId
                && !m.IsDeleted)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                SenderName = m.Sender.Profile != null ? m.Sender.Profile.FullName ?? m.Sender.Email : m.Sender.Email,
                Content = m.Content,
                IsRead = m.IsRead,
                ReadAt = m.ReadAt,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return messages;
    }
}

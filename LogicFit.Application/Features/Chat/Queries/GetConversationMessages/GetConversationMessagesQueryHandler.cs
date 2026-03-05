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

    public GetConversationMessagesQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<ChatMessageDto>> Handle(GetConversationMessagesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Verify conversation exists
        var conversationExists = await _context.ChatConversations
            .AnyAsync(c => c.Id == request.ConversationId
                && c.TenantId == tenantId
                && !c.IsDeleted, cancellationToken);

        if (!conversationExists)
            throw new NotFoundException("ChatConversation", request.ConversationId);

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

using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Chat.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Chat.Queries.GetMyConversations;

public class GetMyConversationsQueryHandler : IRequestHandler<GetMyConversationsQuery, List<ConversationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetMyConversationsQueryHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<List<ConversationDto>> Handle(GetMyConversationsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var currentUserId = Guid.Parse(_currentUserService.UserId!);

        var conversations = await _context.ChatConversations
            .Include(c => c.Coach).ThenInclude(u => u.Profile)
            .Include(c => c.Client).ThenInclude(u => u.Profile)
            .Include(c => c.Messages)
            .Where(c => c.TenantId == tenantId
                && !c.IsDeleted
                && (c.CoachId == currentUserId || c.ClientId == currentUserId))
            .OrderByDescending(c => c.LastMessageAt)
            .Select(c => new ConversationDto
            {
                Id = c.Id,
                CoachId = c.CoachId,
                CoachName = c.Coach.Profile != null ? c.Coach.Profile.FullName ?? c.Coach.Email : c.Coach.Email,
                ClientId = c.ClientId,
                ClientName = c.Client.Profile != null ? c.Client.Profile.FullName ?? c.Client.Email : c.Client.Email,
                LastMessageAt = c.LastMessageAt,
                LastMessagePreview = c.Messages
                    .Where(m => !m.IsDeleted)
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Content)
                    .FirstOrDefault(),
                UnreadCount = c.Messages
                    .Count(m => !m.IsRead && m.SenderId != currentUserId && !m.IsDeleted)
            })
            .ToListAsync(cancellationToken);

        return conversations;
    }
}

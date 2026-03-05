using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Notifications.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Notifications.Queries.GetMyNotifications;

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, List<NotificationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyNotificationsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<NotificationDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId!);

        var query = _context.Notifications
            .Include(n => n.Sender).ThenInclude(s => s.Profile)
            .Include(n => n.Recipient).ThenInclude(r => r.Profile)
            .Where(n => n.RecipientId == userId)
            .AsQueryable();

        if (request.IsRead.HasValue)
            query = query.Where(n => n.IsRead == request.IsRead.Value);

        if (request.Type.HasValue)
            query = query.Where(n => n.Type == request.Type.Value);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                SenderId = n.SenderId,
                SenderName = n.Sender.Profile != null ? n.Sender.Profile.FullName : n.Sender.Email,
                RecipientId = n.RecipientId,
                RecipientName = n.Recipient.Profile != null ? n.Recipient.Profile.FullName : n.Recipient.Email,
                Title = n.Title,
                Body = n.Body,
                Type = n.Type,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}

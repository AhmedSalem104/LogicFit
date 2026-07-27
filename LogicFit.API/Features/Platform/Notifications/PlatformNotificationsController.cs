using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Notifications.DTOs;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using LogicFit.API.Features.Platform.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.API.Features.Platform.Notifications;

[ApiController]
[Route("api/platform/notifications")]
[Authorize(Policy = Permissions.ManagePlatformReports)]
public sealed class PlatformNotificationsController(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<object>> List(
        [FromQuery] string? search = null,
        [FromQuery] NotificationType? type = null,
        [FromQuery] bool? isRead = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PlatformPaging.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId)) return Unauthorized();

        var query = context.Notifications.AsNoTracking().IgnoreQueryFilters()
            .Where(x => x.RecipientId == userId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Title.Contains(search) || x.Body.Contains(search));
        if (type.HasValue) query = query.Where(x => x.Type == type.Value);
        if (isRead.HasValue) query = query.Where(x => x.IsRead == isRead.Value);

        var unreadCount = await context.Notifications.IgnoreQueryFilters()
            .CountAsync(x => x.RecipientId == userId && !x.IsRead, cancellationToken);
        var result = await PlatformPaging.CreateAsync(
            query.OrderByDescending(x => x.CreatedAt)
                .Select(x => new NotificationDto
                {
                    Id = x.Id, SenderId = x.SenderId, RecipientId = x.RecipientId,
                    Title = x.Title, Body = x.Body, Type = x.Type,
                    IsRead = x.IsRead, ReadAt = x.ReadAt, CreatedAt = x.CreatedAt
                }), page, pageSize, cancellationToken);
        return Ok(new { result.Items, result.Page, result.PageSize, result.TotalCount, unreadCount });
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId)) return Unauthorized();
        var notification = await context.Notifications.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.RecipientId == userId, cancellationToken);
        if (notification is null) return NotFound();
        if (!notification.IsRead) { notification.IsRead = true; notification.ReadAt = DateTime.UtcNow; await context.SaveChangesAsync(cancellationToken); }
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId)) return Unauthorized();
        var notifications = await context.Notifications.IgnoreQueryFilters()
            .Where(x => x.RecipientId == userId && !x.IsRead).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var notification in notifications) { notification.IsRead = true; notification.ReadAt = now; }
        if (notifications.Count > 0) await context.SaveChangesAsync(cancellationToken);
        return Ok(new { marked = notifications.Count });
    }
}

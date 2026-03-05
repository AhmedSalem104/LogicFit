using LogicFit.Application.Features.Notifications.Commands.MarkAllAsRead;
using LogicFit.Application.Features.Notifications.Commands.MarkAsRead;
using LogicFit.Application.Features.Notifications.Commands.SendBulkNotification;
using LogicFit.Application.Features.Notifications.Commands.SendNotification;
using LogicFit.Application.Features.Notifications.DTOs;
using LogicFit.Application.Features.Notifications.Queries.GetMyNotifications;
using LogicFit.Application.Features.Notifications.Queries.GetUnreadCount;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Notifications;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetMyNotifications(
        [FromQuery] bool? isRead,
        [FromQuery] NotificationType? type)
    {
        var result = await _mediator.Send(new GetMyNotificationsQuery
        {
            IsRead = isRead,
            Type = type
        });
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var result = await _mediator.Send(new GetUnreadCountQuery());
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> SendNotification(SendNotificationCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<int>> SendBulkNotification(SendBulkNotificationCommand command)
    {
        var count = await _mediator.Send(command);
        return Ok(count);
    }

    [HttpPut("{id}/read")]
    public async Task<ActionResult> MarkAsRead(Guid id)
    {
        await _mediator.Send(new MarkAsReadCommand { NotificationId = id });
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<int>> MarkAllAsRead()
    {
        var count = await _mediator.Send(new MarkAllAsReadCommand());
        return Ok(count);
    }
}

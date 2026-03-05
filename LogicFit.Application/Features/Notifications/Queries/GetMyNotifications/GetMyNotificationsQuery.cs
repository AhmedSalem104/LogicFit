using LogicFit.Application.Features.Notifications.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Notifications.Queries.GetMyNotifications;

public class GetMyNotificationsQuery : IRequest<List<NotificationDto>>
{
    public bool? IsRead { get; set; }
    public NotificationType? Type { get; set; }
}

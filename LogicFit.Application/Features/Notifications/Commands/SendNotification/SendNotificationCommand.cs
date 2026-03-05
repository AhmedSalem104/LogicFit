using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Notifications.Commands.SendNotification;

public class SendNotificationCommand : IRequest<Guid>
{
    public Guid RecipientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.General;
}

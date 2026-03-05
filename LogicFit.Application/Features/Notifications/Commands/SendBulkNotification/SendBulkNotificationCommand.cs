using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Notifications.Commands.SendBulkNotification;

public class SendBulkNotificationCommand : IRequest<int>
{
    public List<Guid> RecipientIds { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.General;
}

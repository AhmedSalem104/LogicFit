using MediatR;

namespace LogicFit.Application.Features.Notifications.Commands.MarkAsRead;

public class MarkAsReadCommand : IRequest<bool>
{
    public Guid NotificationId { get; set; }
}

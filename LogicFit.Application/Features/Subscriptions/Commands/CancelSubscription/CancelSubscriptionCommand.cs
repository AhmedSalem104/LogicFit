using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Commands.CancelSubscription;

public class CancelSubscriptionCommand : IRequest<bool>
{
    public Guid SubscriptionId { get; set; }
}

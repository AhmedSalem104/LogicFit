using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Commands.CreateClientSubscription;

public class CreateClientSubscriptionCommand : IRequest<Guid>
{
    public Guid ClientId { get; set; }
    public Guid PlanId { get; set; }
    public DateTime StartDate { get; set; }
}

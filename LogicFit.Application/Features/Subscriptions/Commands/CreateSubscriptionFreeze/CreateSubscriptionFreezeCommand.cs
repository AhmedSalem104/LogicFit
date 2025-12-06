using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Commands.CreateSubscriptionFreeze;

public class CreateSubscriptionFreezeCommand : IRequest<Guid>
{
    public Guid SubscriptionId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
}

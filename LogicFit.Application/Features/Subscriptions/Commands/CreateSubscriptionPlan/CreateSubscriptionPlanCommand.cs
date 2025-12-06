using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;

public class CreateSubscriptionPlanCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMonths { get; set; }
}

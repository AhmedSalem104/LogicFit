using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Commands.DeleteSubscriptionPlan;

public class DeleteSubscriptionPlanCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

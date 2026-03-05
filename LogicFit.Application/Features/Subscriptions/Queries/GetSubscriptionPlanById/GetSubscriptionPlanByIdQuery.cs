using LogicFit.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Queries.GetSubscriptionPlanById;

public class GetSubscriptionPlanByIdQuery : IRequest<SubscriptionPlanDto?>
{
    public Guid Id { get; set; }
}

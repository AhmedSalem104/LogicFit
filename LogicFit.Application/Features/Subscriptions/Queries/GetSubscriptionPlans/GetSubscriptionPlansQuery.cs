using LogicFit.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Queries.GetSubscriptionPlans;

public class GetSubscriptionPlansQuery : IRequest<List<SubscriptionPlanDto>>
{
}

using LogicFit.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Queries.GetExpiringSubscriptions;

public class GetExpiringSubscriptionsQuery : IRequest<List<ClientSubscriptionDto>>
{
    public int Days { get; set; } = 7;
}

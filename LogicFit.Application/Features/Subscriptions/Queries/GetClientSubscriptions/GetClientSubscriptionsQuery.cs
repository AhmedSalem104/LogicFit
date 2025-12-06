using LogicFit.Application.Features.Subscriptions.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Queries.GetClientSubscriptions;

public class GetClientSubscriptionsQuery : IRequest<List<ClientSubscriptionDto>>
{
    public Guid? ClientId { get; set; }
    public SubscriptionStatus? Status { get; set; }
}

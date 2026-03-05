using LogicFit.Application.Features.ClientDashboard.DTOs;
using MediatR;

namespace LogicFit.Application.Features.ClientDashboard.Queries.GetMySubscriptions;

public class GetMySubscriptionsQuery : IRequest<List<MySubscriptionSummaryDto>>
{
}

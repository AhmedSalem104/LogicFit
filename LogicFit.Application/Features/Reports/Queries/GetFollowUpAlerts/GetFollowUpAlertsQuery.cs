using LogicFit.Application.Features.Reports.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Reports.Queries.GetFollowUpAlerts;

public sealed class GetFollowUpAlertsQuery : IRequest<FollowUpAlertsDto>
{
    public int ExpiringWithinDays { get; init; } = 7;
    public int InactiveAfterDays { get; init; } = 7;
    public int Limit { get; init; } = 100;
}

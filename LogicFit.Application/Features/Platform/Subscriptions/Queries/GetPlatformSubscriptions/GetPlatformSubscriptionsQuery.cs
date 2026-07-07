using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Platform.Subscriptions.Queries.GetPlatformSubscriptions;

public class GetPlatformSubscriptionsQuery : IRequest<List<PlatformSubscriptionDto>>
{
    public TenantSubscriptionStatus? Status { get; set; }
}

public class PlatformSubscriptionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public TenantSubscriptionStatus Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public bool AutoRenew { get; set; }
}

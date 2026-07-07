using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.TenantBilling.DTOs;

public class MySubscriptionDto
{
    public bool HasSubscription { get; set; }
    public Guid? SubscriptionId { get; set; }
    public Guid? PlanId { get; set; }
    public string? PlanName { get; set; }
    public TenantSubscriptionStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public int? RemainingDays { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public bool AutoRenew { get; set; }
    public List<string> Features { get; set; } = new();

    // Limits (null = unlimited) and current live usage
    public UsageLineDto Members { get; set; } = new();
    public UsageLineDto Coaches { get; set; } = new();
    public UsageLineDto Branches { get; set; } = new();
    public UsageLineDto Employees { get; set; } = new();
}

public class UsageLineDto
{
    public int Used { get; set; }
    public int? Limit { get; set; }
}

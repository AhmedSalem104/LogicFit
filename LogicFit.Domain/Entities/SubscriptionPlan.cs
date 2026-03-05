using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class SubscriptionPlan : TenantAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMonths { get; set; }
    public string? Description { get; set; }
    public string? Features { get; set; } // JSON array of feature strings
    public int MaxFreezeDays { get; set; }
    public int MaxFreezeCount { get; set; }
    public bool IsActive { get; set; } = true;
    public int? SessionsPerWeek { get; set; }
    public bool InBodyIncluded { get; set; }
    public bool PrivateCoach { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<ClientSubscription> Subscriptions { get; set; } = new List<ClientSubscription>();
}

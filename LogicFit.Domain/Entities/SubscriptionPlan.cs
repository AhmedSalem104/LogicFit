using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class SubscriptionPlan : TenantAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMonths { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<ClientSubscription> Subscriptions { get; set; } = new List<ClientSubscription>();
}

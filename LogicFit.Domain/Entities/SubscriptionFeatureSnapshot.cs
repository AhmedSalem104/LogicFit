using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>Immutable feature entitlement captured when a platform subscription is activated.</summary>
public class SubscriptionFeatureSnapshot : BaseEntity
{
    public Guid TenantSubscriptionId { get; set; }
    public string FeatureKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int? LimitValue { get; set; }
    public DateTime CapturedAt { get; set; }

    public virtual TenantSubscription TenantSubscription { get; set; } = null!;
}

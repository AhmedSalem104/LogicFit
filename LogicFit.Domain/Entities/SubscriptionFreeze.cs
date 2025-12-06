using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class SubscriptionFreeze : TenantAuditableEntity
{
    public Guid SubscriptionId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }

    // Navigation Properties
    public virtual ClientSubscription Subscription { get; set; } = null!;
}

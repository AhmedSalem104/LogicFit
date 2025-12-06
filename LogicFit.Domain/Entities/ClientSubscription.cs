using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class ClientSubscription : TenantAuditableEntity
{
    public Guid ClientId { get; set; }
    public Guid PlanId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    public Guid? SalesCoachId { get; set; }

    // Navigation Properties
    public virtual User Client { get; set; } = null!;
    public virtual SubscriptionPlan Plan { get; set; } = null!;
    public virtual User? SalesCoach { get; set; }
    public virtual ICollection<SubscriptionFreeze> Freezes { get; set; } = new List<SubscriptionFreeze>();
}

using LogicFit.Domain.Common;
using LogicFit.Domain.Common.Interfaces;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

/// <summary>
/// A gym's subscription to the platform (distinct from a gym member's subscription). Platform-owned:
/// keyed by TenantId but NOT tenant query-filtered — the platform reads across all tenants, and
/// tenant-owner queries must filter by TenantId explicitly.
/// </summary>
public class TenantSubscription : AuditableEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public TenantSubscriptionStatus Status { get; set; } = TenantSubscriptionStatus.PendingPayment;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? RenewDate { get; set; }

    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public bool AutoRenew { get; set; }

    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public string? Notes { get; set; }

    // When the "expiring soon" reminder was last sent for the current period (reset on renewal).
    public DateTime? ReminderSentAt { get; set; }

    // Optimistic concurrency to prevent double-activation during payment approval.
    public byte[]? RowVersion { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Plan Plan { get; set; } = null!;
}

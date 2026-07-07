using LogicFit.Domain.Common;
using LogicFit.Domain.Common.Interfaces;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

/// <summary>
/// A manual payment submitted by a gym owner (proof-of-payment image + reference) for platform review.
/// Platform-owned: keyed by TenantId but not tenant query-filtered.
/// </summary>
public class PaymentRequest : AuditableEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Guid? TenantSubscriptionId { get; set; }
    public Guid PlanId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public Guid? PaymentMethodId { get; set; }
    public string? TransactionNumber { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? ProofFileUrl { get; set; }
    public string? Notes { get; set; }
    public PaymentRequestStatus Status { get; set; } = PaymentRequestStatus.Pending;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectReason { get; set; }

    // Optimistic concurrency to make approve/reject idempotent under races.
    public byte[]? RowVersion { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Plan Plan { get; set; } = null!;
    public virtual TenantSubscription? TenantSubscription { get; set; }
    public virtual TenantPaymentMethod? PaymentMethod { get; set; }
}

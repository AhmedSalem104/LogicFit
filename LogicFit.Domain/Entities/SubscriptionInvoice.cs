using LogicFit.Domain.Common;
using LogicFit.Domain.Common.Interfaces;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

/// <summary>An invoice for a platform subscription period (issued even for manual payments). Platform-owned.</summary>
public class SubscriptionInvoice : AuditableEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Guid TenantSubscriptionId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public SubscriptionInvoiceStatus Status { get; set; } = SubscriptionInvoiceStatus.Unpaid;
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

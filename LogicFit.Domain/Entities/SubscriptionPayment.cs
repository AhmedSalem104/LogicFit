using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>A settled payment record created when a PaymentRequest is approved. Platform-owned.</summary>
public class SubscriptionPayment : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid TenantSubscriptionId { get; set; }
    public Guid PaymentRequestId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public Guid? PaymentMethodId { get; set; }
    public string? TransactionNumber { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime ApprovedAt { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
}

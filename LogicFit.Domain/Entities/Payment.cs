using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class Payment : TenantAuditableEntity
{
    public Guid? InvoiceId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ClientId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime ReceivedAt { get; set; }
    public Guid? ReceivedById { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
    public string? ReferenceNumber { get; set; }

    public virtual Invoice? Invoice { get; set; }
    public virtual ClientSubscription? Subscription { get; set; }
    public virtual Branch? Branch { get; set; }
    public virtual User? Client { get; set; }
    public virtual User? ReceivedBy { get; set; }
}

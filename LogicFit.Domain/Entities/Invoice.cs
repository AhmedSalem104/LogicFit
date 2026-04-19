using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class Invoice : TenantAuditableEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid? ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public Guid? CouponId { get; set; }
    public string? Notes { get; set; }
    public string? PdfUrl { get; set; }

    public decimal RemainingAmount => Total - AmountPaid;

    public virtual User? Client { get; set; }
    public virtual Branch? Branch { get; set; }
    public virtual Coupon? Coupon { get; set; }
    public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class Sale : TenantAuditableEntity
{
    public string SaleNumber { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? CashierId { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? CouponId { get; set; }
    public string? Notes { get; set; }

    public virtual Branch Branch { get; set; } = null!;
    public virtual User? Client { get; set; }
    public virtual User? Cashier { get; set; }
    public virtual Invoice? Invoice { get; set; }
    public virtual Coupon? Coupon { get; set; }
    public virtual ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}

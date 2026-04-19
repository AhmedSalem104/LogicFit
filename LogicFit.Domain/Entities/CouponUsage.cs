using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class CouponUsage : TenantAuditableEntity
{
    public Guid CouponId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public DateTime UsedAt { get; set; }
    public decimal DiscountApplied { get; set; }

    public virtual Coupon Coupon { get; set; } = null!;
    public virtual User? User { get; set; }
    public virtual Invoice? Invoice { get; set; }
}

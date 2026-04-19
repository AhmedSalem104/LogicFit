using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class ClientSubscription : TenantAuditableEntity
{
    public Guid ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid PlanId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    public Guid? SalesCoachId { get; set; }

    // Payment
    public PaymentMethod? PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Discount { get; set; }
    public string? Notes { get; set; }

    // Renewal
    public Guid? RenewedFromId { get; set; }

    // Finance links
    public Guid? CouponId { get; set; }
    public decimal TaxAmount { get; set; }
    public Guid? InvoiceId { get; set; }

    // Navigation Properties
    public virtual User Client { get; set; } = null!;
    public virtual Branch? Branch { get; set; }
    public virtual SubscriptionPlan Plan { get; set; } = null!;
    public virtual User? SalesCoach { get; set; }
    public virtual ClientSubscription? RenewedFrom { get; set; }
    public virtual ICollection<SubscriptionFreeze> Freezes { get; set; } = new List<SubscriptionFreeze>();
    public virtual Coupon? Coupon { get; set; }
    public virtual Invoice? Invoice { get; set; }
}

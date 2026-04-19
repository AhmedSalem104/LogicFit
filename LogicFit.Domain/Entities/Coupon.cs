using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class Coupon : TenantAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinimumAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public int? MaxUsesPerUser { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public CouponApplicability ApplicableTo { get; set; } = CouponApplicability.All;
    public bool IsActive { get; set; } = true;

    public virtual ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();
}

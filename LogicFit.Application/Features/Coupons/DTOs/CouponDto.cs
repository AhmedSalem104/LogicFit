using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Coupons.DTOs;

public class CouponDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DiscountType DiscountType { get; set; }
    public string DiscountTypeName => DiscountType.ToString();
    public decimal DiscountValue { get; set; }
    public decimal? MinimumAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public int? MaxUsesPerUser { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public CouponApplicability ApplicableTo { get; set; }
    public string ApplicableToName => ApplicableTo.ToString();
    public bool IsActive { get; set; }
    public bool IsExpired => EndDate.HasValue && EndDate.Value < DateTime.UtcNow;
    public bool UsesLimitReached => MaxUses.HasValue && UsedCount >= MaxUses.Value;
}

public class ValidateCouponResultDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public CouponDto? Coupon { get; set; }
    public decimal? EstimatedDiscount { get; set; }
}

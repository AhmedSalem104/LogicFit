using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Coupons.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Coupons.Queries.ValidateCoupon;

public class ValidateCouponQueryHandler : IRequestHandler<ValidateCouponQuery, ValidateCouponResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public ValidateCouponQueryHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<ValidateCouponResultDto> Handle(ValidateCouponQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var code = request.Code.Trim().ToUpperInvariant();

        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Code == code, cancellationToken);

        if (coupon == null)
            return new ValidateCouponResultDto { IsValid = false, ErrorMessage = "Coupon not found" };

        var now = _dateTimeService.UtcNow;

        if (!coupon.IsActive)
            return new ValidateCouponResultDto { IsValid = false, ErrorMessage = "Coupon is inactive" };
        if (coupon.StartDate.HasValue && coupon.StartDate.Value > now)
            return new ValidateCouponResultDto { IsValid = false, ErrorMessage = "Coupon is not yet active" };
        if (coupon.EndDate.HasValue && coupon.EndDate.Value < now)
            return new ValidateCouponResultDto { IsValid = false, ErrorMessage = "Coupon has expired" };
        if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses.Value)
            return new ValidateCouponResultDto { IsValid = false, ErrorMessage = "Coupon usage limit reached" };
        if (coupon.MinimumAmount.HasValue && request.Amount < coupon.MinimumAmount.Value)
            return new ValidateCouponResultDto { IsValid = false, ErrorMessage = $"Minimum amount for this coupon is {coupon.MinimumAmount.Value:C}" };
        if (request.Context.HasValue && coupon.ApplicableTo != CouponApplicability.All && coupon.ApplicableTo != request.Context.Value)
            return new ValidateCouponResultDto { IsValid = false, ErrorMessage = "Coupon not applicable to this type of purchase" };

        decimal estimatedDiscount = coupon.DiscountType == DiscountType.Percentage
            ? request.Amount * (coupon.DiscountValue / 100m)
            : coupon.DiscountValue;

        if (coupon.MaxDiscountAmount.HasValue && estimatedDiscount > coupon.MaxDiscountAmount.Value)
            estimatedDiscount = coupon.MaxDiscountAmount.Value;

        return new ValidateCouponResultDto
        {
            IsValid = true,
            EstimatedDiscount = estimatedDiscount,
            Coupon = new CouponDto
            {
                Id = coupon.Id,
                TenantId = coupon.TenantId,
                Code = coupon.Code,
                Description = coupon.Description,
                DiscountType = coupon.DiscountType,
                DiscountValue = coupon.DiscountValue,
                MinimumAmount = coupon.MinimumAmount,
                MaxDiscountAmount = coupon.MaxDiscountAmount,
                MaxUses = coupon.MaxUses,
                UsedCount = coupon.UsedCount,
                MaxUsesPerUser = coupon.MaxUsesPerUser,
                StartDate = coupon.StartDate,
                EndDate = coupon.EndDate,
                ApplicableTo = coupon.ApplicableTo,
                IsActive = coupon.IsActive
            }
        };
    }
}

using LogicFit.Application.Features.Coupons.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Coupons.Queries.ValidateCoupon;

public class ValidateCouponQuery : IRequest<ValidateCouponResultDto>
{
    public string Code { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public CouponApplicability? Context { get; set; }

    /// <summary>Optional client the coupon would be redeemed for; enables the per-user usage check.</summary>
    public Guid? ClientId { get; set; }
}

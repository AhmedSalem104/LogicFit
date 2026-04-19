using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Coupons.Commands.UpdateCoupon;

public class UpdateCouponCommand : IRequest
{
    public Guid Id { get; set; }
    public string? Description { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinimumAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? MaxUses { get; set; }
    public int? MaxUsesPerUser { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public CouponApplicability ApplicableTo { get; set; }
    public bool IsActive { get; set; }
}

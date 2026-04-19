using LogicFit.Application.Features.Coupons.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Coupons.Queries.GetCoupons;

public class GetCouponsQuery : IRequest<List<CouponDto>>
{
    public bool? IsActive { get; set; }
    public string? Search { get; set; }
}

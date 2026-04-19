using MediatR;

namespace LogicFit.Application.Features.Coupons.Commands.DeleteCoupon;

public class DeleteCouponCommand : IRequest
{
    public Guid Id { get; set; }
}

using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Coupons.Commands.UpdateCoupon;

public class UpdateCouponCommandHandler : IRequestHandler<UpdateCouponCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateCouponCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(UpdateCouponCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Coupon", request.Id);

        coupon.Description = request.Description;
        coupon.DiscountType = request.DiscountType;
        coupon.DiscountValue = request.DiscountValue;
        coupon.MinimumAmount = request.MinimumAmount;
        coupon.MaxDiscountAmount = request.MaxDiscountAmount;
        coupon.MaxUses = request.MaxUses;
        coupon.MaxUsesPerUser = request.MaxUsesPerUser;
        coupon.StartDate = request.StartDate;
        coupon.EndDate = request.EndDate;
        coupon.ApplicableTo = request.ApplicableTo;
        coupon.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Coupons.Commands.CreateCoupon;

public class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateCouponCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var code = request.Code.Trim().ToUpperInvariant();

        var exists = await _context.Coupons.AnyAsync(c => c.TenantId == tenantId && c.Code == code, cancellationToken);
        if (exists)
            throw new ConflictException("A coupon with this code already exists");

        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Description = request.Description,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MinimumAmount = request.MinimumAmount,
            MaxDiscountAmount = request.MaxDiscountAmount,
            MaxUses = request.MaxUses,
            MaxUsesPerUser = request.MaxUsesPerUser,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ApplicableTo = request.ApplicableTo,
            IsActive = request.IsActive
        };

        _context.Coupons.Add(coupon);
        await _context.SaveChangesAsync(cancellationToken);
        return coupon.Id;
    }
}

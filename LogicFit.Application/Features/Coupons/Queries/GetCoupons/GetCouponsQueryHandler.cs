using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Coupons.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Coupons.Queries.GetCoupons;

public class GetCouponsQueryHandler : IRequestHandler<GetCouponsQuery, List<CouponDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetCouponsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<CouponDto>> Handle(GetCouponsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Coupons.Where(c => c.TenantId == tenantId).AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(c => c.Code.Contains(term) || (c.Description != null && c.Description.Contains(term)));
        }

        var coupons = await query.OrderByDescending(c => c.CreatedAt).ToListAsync(cancellationToken);

        return coupons.Select(c => new CouponDto
        {
            Id = c.Id,
            TenantId = c.TenantId,
            Code = c.Code,
            Description = c.Description,
            DiscountType = c.DiscountType,
            DiscountValue = c.DiscountValue,
            MinimumAmount = c.MinimumAmount,
            MaxDiscountAmount = c.MaxDiscountAmount,
            MaxUses = c.MaxUses,
            UsedCount = c.UsedCount,
            MaxUsesPerUser = c.MaxUsesPerUser,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            ApplicableTo = c.ApplicableTo,
            IsActive = c.IsActive
        }).ToList();
    }
}

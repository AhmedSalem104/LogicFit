using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Foods.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Foods.Queries.GetFoods;

public class GetFoodsQueryHandler : IRequestHandler<GetFoodsQuery, List<FoodDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetFoodsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<FoodDto>> Handle(GetFoodsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Foods
            .Where(f => f.TenantId == null || f.TenantId == tenantId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.Category))
            query = query.Where(f => f.Category == request.Category);

        if (!string.IsNullOrEmpty(request.SearchTerm))
            query = query.Where(f => f.Name.Contains(request.SearchTerm));

        if (request.IsVerified.HasValue)
            query = query.Where(f => f.IsVerified == request.IsVerified.Value);

        return await query
            .Select(f => new FoodDto
            {
                Id = f.Id,
                TenantId = f.TenantId,
                Name = f.Name,
                Category = f.Category,
                CaloriesPer100g = f.CaloriesPer100g,
                ProteinPer100g = f.ProteinPer100g,
                CarbsPer100g = f.CarbsPer100g,
                FatsPer100g = f.FatsPer100g,
                FiberPer100g = f.FiberPer100g,
                AlternativeGroupId = f.AlternativeGroupId,
                IsVerified = f.IsVerified
            })
            .ToListAsync(cancellationToken);
    }
}

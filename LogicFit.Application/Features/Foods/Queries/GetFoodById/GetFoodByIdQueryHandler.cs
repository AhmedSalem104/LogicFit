using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Foods.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Foods.Queries.GetFoodById;

public class GetFoodByIdQueryHandler : IRequestHandler<GetFoodByIdQuery, FoodDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetFoodByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<FoodDto?> Handle(GetFoodByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        return await _context.Foods
            .Where(f => f.Id == request.Id && (f.TenantId == null || f.TenantId == tenantId))
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
            .FirstOrDefaultAsync(cancellationToken);
    }
}

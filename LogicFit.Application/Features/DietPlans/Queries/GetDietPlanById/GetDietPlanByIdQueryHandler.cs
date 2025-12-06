using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.DietPlans.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Queries.GetDietPlanById;

public class GetDietPlanByIdQueryHandler : IRequestHandler<GetDietPlanByIdQuery, DietPlanDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetDietPlanByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<DietPlanDto?> Handle(GetDietPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        return await _context.DietPlans
            .Include(p => p.Coach).ThenInclude(c => c.Profile)
            .Include(p => p.Client).ThenInclude(c => c.Profile)
            .Include(p => p.Meals)
                .ThenInclude(m => m.Items)
                    .ThenInclude(i => i.Food)
            .Where(p => p.Id == request.Id && p.TenantId == tenantId)
            .Select(p => new DietPlanDto
            {
                Id = p.Id,
                TenantId = p.TenantId,
                CoachId = p.CoachId,
                CoachName = p.Coach.Profile != null ? p.Coach.Profile.FullName : p.Coach.Email,
                ClientId = p.ClientId,
                ClientName = p.Client.Profile != null ? p.Client.Profile.FullName : p.Client.Email,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status,
                TargetCalories = p.TargetCalories,
                TargetProtein = p.TargetProtein,
                TargetCarbs = p.TargetCarbs,
                TargetFats = p.TargetFats,
                Meals = p.Meals.Select(m => new DailyMealDto
                {
                    Id = m.Id,
                    PlanId = m.PlanId,
                    Name = m.Name,
                    OrderIndex = m.OrderIndex,
                    Items = m.Items.Select(i => new MealItemDto
                    {
                        Id = i.Id,
                        MealId = i.MealId,
                        FoodId = i.FoodId,
                        FoodName = i.Food.Name,
                        AssignedQuantity = i.AssignedQuantity,
                        CalcCalories = i.CalcCalories,
                        CalcProtein = i.CalcProtein,
                        CalcCarbs = i.CalcCarbs,
                        CalcFats = i.CalcFats
                    }).ToList()
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}

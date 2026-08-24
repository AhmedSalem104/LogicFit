using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.DietPlans.DTOs;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Queries.GetDietPlanById;

public class GetDietPlanByIdQueryHandler : IRequestHandler<GetDietPlanByIdQuery, DietPlanDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetDietPlanByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<DietPlanDto?> Handle(GetDietPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            throw new ForbiddenException("An authenticated workspace user is required.");
        var role = await _context.Users.Where(u => u.Id == currentUserId && u.TenantId == tenantId && u.IsActive)
            .Select(u => (UserRole?)u.Role).FirstOrDefaultAsync(cancellationToken);
        if (!role.HasValue)
            throw new ForbiddenException("The authenticated user is not active in this workspace.");

        return await _context.DietPlans
            .Include(p => p.Coach).ThenInclude(c => c.Profile)
            .Include(p => p.Client).ThenInclude(c => c.Profile)
            .Include(p => p.Meals)
                .ThenInclude(m => m.Items)
                    .ThenInclude(i => i.Food)
            .Where(p => p.Id == request.Id && p.TenantId == tenantId
                && ((role == UserRole.Client && p.ClientId == currentUserId && p.Status == PlanStatus.Active)
                    || (role == UserRole.Owner || role == UserRole.Manager || role == UserRole.FreelanceOwner)
                    || ((role == UserRole.Coach || role == UserRole.Trainer || role == UserRole.FreelanceCoach)
                        && _context.CoachClients.Any(cc => cc.TenantId == tenantId
                            && cc.CoachId == currentUserId
                            && cc.ClientId == p.ClientId
                            && cc.IsActive
                            && cc.UnassignedAt == null))))
            .Select(p => new DietPlanDto
            {
                Id = p.Id,
                TenantId = p.TenantId,
                CoachId = p.CoachId,
                CoachName = p.Coach.Profile != null ? p.Coach.Profile.FullName : p.Coach.Email,
                ClientId = p.ClientId,
                ClientName = p.Client.Profile != null ? p.Client.Profile.FullName : p.Client.Email,
                Name = p.Name,
                Description = p.Description,
                MealsPerDay = p.MealsPerDay,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status,
                TargetCalories = p.TargetCalories,
                TargetProtein = p.TargetProtein,
                TargetCarbs = p.TargetCarbs,
                TargetFats = p.TargetFats,
                CalorieGoal = p.CalorieGoal,
                CalorieAdjustment = p.CalorieAdjustment,
                CalculatorMetadata = p.CalculatorMetadata,
                Notes = p.Notes,
                Version = p.Version,
                Meals = p.Meals.Select(m => new DailyMealDto
                {
                    Id = m.Id,
                    PlanId = m.PlanId,
                    Name = m.Name,
                    OrderIndex = m.OrderIndex,
                    Time = m.Time,
                    Notes = m.Notes,
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
                        CalcFats = i.CalcFats,
                        ServingUnit = i.ServingUnit,
                        Notes = i.Notes,
                        FoodServingSizeSnapshot = i.FoodServingSizeSnapshot
                    }).ToList()
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}

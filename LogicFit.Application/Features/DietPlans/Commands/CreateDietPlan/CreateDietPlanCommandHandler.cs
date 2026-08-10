using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.DietPlans.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Commands.CreateDietPlan;

public class CreateDietPlanCommandHandler : IRequestHandler<CreateDietPlanCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICoachPlanAccessService _accessService;

    public CreateDietPlanCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _accessService = accessService;
    }

    public async Task<Guid> Handle(CreateDietPlanCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            throw new ForbiddenException("An authenticated coach is required.");

        var tenantId = _tenantService.GetCurrentTenantId();
        await _accessService.EnsureCanManageClientAsync(request.ClientId, cancellationToken);
        await EnsureFoodsBelongToTenantAsync(request.Meals, tenantId, cancellationToken);

        var plan = new DietPlan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CoachId = currentUserId,
            ClientId = request.ClientId,
            Name = request.Name,
            Description = request.Description,
            MealsPerDay = request.MealsPerDay ?? (request.Meals.Count == 0 ? null : request.Meals.Count),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status ?? PlanStatus.Active,
            TargetCalories = request.TargetCalories,
            TargetProtein = request.TargetProtein,
            TargetCarbs = request.TargetCarbs,
            TargetFats = request.TargetFats
        };

        foreach (var mealInput in request.Meals)
        {
            var meal = new DailyMeal
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = mealInput.Name,
                OrderIndex = mealInput.OrderIndex,
                Time = mealInput.Time
            };

            foreach (var itemInput in mealInput.Items)
            {
                var food = await _context.Foods
                    .FirstAsync(f => f.Id == itemInput.FoodId, cancellationToken);
                meal.Items.Add(CreateMealItem(tenantId, itemInput, food));
            }

            plan.Meals.Add(meal);
        }

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        _context.DietPlans.Add(plan);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return plan.Id;
    }

    private async Task EnsureFoodsBelongToTenantAsync(
        IEnumerable<DietMealInputDto> meals,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var foodIds = meals.SelectMany(m => m.Items).Select(i => i.FoodId).Distinct().ToList();
        if (foodIds.Count == 0)
            return;

        var availableIds = await _context.Foods
            .Where(f => foodIds.Contains(f.Id) && !f.IsDeleted && (f.TenantId == null || f.TenantId == tenantId))
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);
        var missingId = foodIds.FirstOrDefault(id => !availableIds.Contains(id));
        if (missingId != 0)
            throw new NotFoundException("Food", missingId);
    }

    private static MealItem CreateMealItem(Guid tenantId, DietMealItemInputDto input, Food food)
    {
        var ratio = input.AssignedQuantity / 100.0;
        return new MealItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FoodId = input.FoodId,
            AssignedQuantity = input.AssignedQuantity,
            CalcCalories = food.CaloriesPer100g * ratio,
            CalcProtein = food.ProteinPer100g * ratio,
            CalcCarbs = food.CarbsPer100g * ratio,
            CalcFats = food.FatsPer100g * ratio
        };
    }
}

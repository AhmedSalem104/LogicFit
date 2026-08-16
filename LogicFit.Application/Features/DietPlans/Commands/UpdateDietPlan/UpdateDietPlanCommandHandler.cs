using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.DietPlans.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Commands.UpdateDietPlan;

public class UpdateDietPlanCommandHandler : IRequestHandler<UpdateDietPlanCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICoachPlanAccessService _accessService;

    public UpdateDietPlanCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _accessService = accessService;
    }

    public async Task<bool> Handle(UpdateDietPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var plan = await _context.DietPlans
            .Include(p => p.Meals)
                .ThenInclude(m => m.Items)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId, cancellationToken);

        if (plan == null)
            throw new NotFoundException("DietPlan", request.Id);

        if (request.ExpectedVersion.HasValue && request.ExpectedVersion.Value != plan.Version)
            throw new ConflictException("This nutrition plan was changed by another user. Reload it before saving.");

        await _accessService.EnsureCanManageDietPlanAsync(request.Id, cancellationToken);
        if (request.ClientId.HasValue && request.ClientId.Value != plan.ClientId)
            throw new ConflictException("A nutrition plan cannot be moved to another client. Duplicate it instead.");
        if (request.Meals != null)
        {
            await EnsureFoodsBelongToTenantAsync(request.Meals, tenantId, cancellationToken);
            ReconcileMeals(plan, request.Meals, tenantId);
        }

        plan.Name = request.Name;
        plan.Description = request.Description;
        if (request.MealsPerDay.HasValue)
            plan.MealsPerDay = request.MealsPerDay.Value;
        plan.StartDate = request.StartDate;
        plan.EndDate = request.EndDate;
        plan.TargetCalories = request.TargetCalories;
        plan.TargetProtein = request.TargetProtein;
        plan.TargetCarbs = request.TargetCarbs;
        plan.TargetFats = request.TargetFats;
        plan.CalorieGoal = request.CalorieGoal;
        plan.CalorieAdjustment = request.CalorieAdjustment;
        plan.CalculatorMetadata = request.CalculatorMetadata;
        plan.Notes = request.Notes?.Trim();
        plan.Version++;
        if (request.Status.HasValue)
            plan.Status = request.Status.Value;

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    private void ReconcileMeals(DietPlan plan, IReadOnlyCollection<DietMealInputDto> requested, Guid tenantId)
    {
        var requestedMealIds = requested.Where(m => m.Id.HasValue).Select(m => m.Id!.Value).ToHashSet();
        foreach (var existingMeal in plan.Meals.Where(m => !requestedMealIds.Contains(m.Id)).ToList())
        {
            // Meal logs reference the planned item. Soft-delete omitted children so the
            // historical log remains readable after a plan revision.
            existingMeal.IsDeleted = true;
            existingMeal.DeletedAt = DateTime.UtcNow;
            foreach (var item in existingMeal.Items)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.UtcNow;
            }
        }

        foreach (var mealInput in requested)
        {
            DailyMeal meal;
            if (mealInput.Id.HasValue)
            {
                meal = plan.Meals.FirstOrDefault(m => m.Id == mealInput.Id.Value)
                    ?? throw new NotFoundException("DailyMeal", mealInput.Id.Value);
                meal.IsDeleted = false;
                meal.DeletedAt = null;
                meal.Name = mealInput.Name;
                meal.OrderIndex = mealInput.OrderIndex;
                meal.Time = mealInput.Time;
                meal.Notes = mealInput.Notes?.Trim();
            }
            else
            {
                meal = new DailyMeal
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PlanId = plan.Id,
                    Name = mealInput.Name,
                    OrderIndex = mealInput.OrderIndex,
                    Time = mealInput.Time,
                    Notes = mealInput.Notes?.Trim()
                };
                plan.Meals.Add(meal);
            }

            var requestedItemIds = mealInput.Items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
            foreach (var existingItem in meal.Items.Where(i => !requestedItemIds.Contains(i.Id)).ToList())
            {
                existingItem.IsDeleted = true;
                existingItem.DeletedAt = DateTime.UtcNow;
            }

            foreach (var itemInput in mealInput.Items)
            {
                MealItem item;
                if (itemInput.Id.HasValue)
                {
                    item = meal.Items.FirstOrDefault(i => i.Id == itemInput.Id.Value)
                        ?? throw new NotFoundException("MealItem", itemInput.Id.Value);
                    item.IsDeleted = false;
                    item.DeletedAt = null;
                }
                else
                {
                    item = new MealItem
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        MealId = meal.Id
                    };
                    meal.Items.Add(item);
                }

                var food = _context.Foods.Local.FirstOrDefault(f => f.Id == itemInput.FoodId)
                    ?? _context.Foods.First(f => f.Id == itemInput.FoodId);
                var servingSize = food.ServingSize is > 0 ? food.ServingSize.Value : 100d;
                var ratio = itemInput.AssignedQuantity / servingSize;
                item.FoodId = itemInput.FoodId;
                item.AssignedQuantity = itemInput.AssignedQuantity;
                item.ServingUnit = itemInput.ServingUnit ?? food.ServingUnit;
                item.Notes = itemInput.Notes?.Trim();
                item.FoodServingSizeSnapshot = servingSize;
                item.CalcCalories = food.CaloriesPer100g * ratio;
                item.CalcProtein = food.ProteinPer100g * ratio;
                item.CalcCarbs = food.CarbsPer100g * ratio;
                item.CalcFats = food.FatsPer100g * ratio;
            }
        }
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
}

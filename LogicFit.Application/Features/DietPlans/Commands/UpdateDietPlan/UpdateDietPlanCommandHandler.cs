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

        await _accessService.EnsureCanManageDietPlanAsync(request.Id, cancellationToken);
        if (request.ClientId.HasValue && request.ClientId.Value != plan.ClientId)
        {
            await _accessService.EnsureCanManageClientAsync(request.ClientId.Value, cancellationToken);
            plan.ClientId = request.ClientId.Value;
        }
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
            _context.MealItems.RemoveRange(existingMeal.Items);
            _context.DailyMeals.Remove(existingMeal);
        }

        foreach (var mealInput in requested)
        {
            DailyMeal meal;
            if (mealInput.Id.HasValue)
            {
                meal = plan.Meals.FirstOrDefault(m => m.Id == mealInput.Id.Value)
                    ?? throw new NotFoundException("DailyMeal", mealInput.Id.Value);
                meal.Name = mealInput.Name;
                meal.OrderIndex = mealInput.OrderIndex;
                meal.Time = mealInput.Time;
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
                    Time = mealInput.Time
                };
                plan.Meals.Add(meal);
            }

            var requestedItemIds = mealInput.Items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
            foreach (var existingItem in meal.Items.Where(i => !requestedItemIds.Contains(i.Id)).ToList())
                _context.MealItems.Remove(existingItem);

            foreach (var itemInput in mealInput.Items)
            {
                MealItem item;
                if (itemInput.Id.HasValue)
                {
                    item = meal.Items.FirstOrDefault(i => i.Id == itemInput.Id.Value)
                        ?? throw new NotFoundException("MealItem", itemInput.Id.Value);
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
                var ratio = itemInput.AssignedQuantity / 100.0;
                item.FoodId = itemInput.FoodId;
                item.AssignedQuantity = itemInput.AssignedQuantity;
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

using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.MealLogs.Commands.LogMeal;

public class LogMealCommandHandler : IRequestHandler<LogMealCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public LogMealCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Guid> Handle(LogMealCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!Guid.TryParse(_currentUserService.UserId, out var clientId))
            throw new ForbiddenException("An authenticated client is required.");

        var mealItem = await _context.MealItems
            .Include(mi => mi.Meal)
            .Include(mi => mi.Food)
            .FirstOrDefaultAsync(mi => mi.Id == request.MealItemId && mi.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("MealItem", request.MealItemId);

        // The meal item must belong to a diet plan owned by the signed-in client.
        var ownsPlan = await _context.DietPlans.AnyAsync(
            p => p.Id == mealItem.Meal.PlanId && p.TenantId == tenantId && p.ClientId == clientId, cancellationToken);
        if (!ownsPlan)
            throw new ForbiddenException("This meal is not part of your diet plan");

        if (request.AlternativeFoodId.HasValue)
        {
            var foodExists = await _context.Foods.AnyAsync(
                f => f.Id == request.AlternativeFoodId.Value && (f.TenantId == tenantId || f.TenantId == null),
                cancellationToken);
            if (!foodExists)
                throw new NotFoundException("Food", request.AlternativeFoodId.Value);
        }

        var food = mealItem.Food;
        if (request.AlternativeFoodId.HasValue)
        {
            food = await _context.Foods.FirstAsync(f => f.Id == request.AlternativeFoodId.Value, cancellationToken);
        }
        var servingSize = food.ServingSize is > 0 ? food.ServingSize.Value : 100d;

        var log = new MealLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = clientId,
            MealItemId = mealItem.Id,
            ConsumedQuantity = request.ConsumedQuantity,
            ConsumedAt = request.ConsumedAt ?? _dateTimeService.UtcNow,
            AlternativeFoodId = request.AlternativeFoodId,
            MealNameSnapshot = mealItem.Meal.Name,
            FoodNameSnapshot = food.Name,
            FoodUnitSnapshot = food.ServingUnit,
            FoodServingSizeSnapshot = servingSize,
            FoodCaloriesSnapshot = food.CaloriesPer100g,
            FoodProteinSnapshot = food.ProteinPer100g,
            FoodCarbsSnapshot = food.CarbsPer100g,
            FoodFatsSnapshot = food.FatsPer100g
        };

        _context.MealLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        return log.Id;
    }
}

using LogicFit.Application.Features.MealLogs.DTOs;
using LogicFit.Domain.Entities;

namespace LogicFit.Application.Features.MealLogs;

/// <summary>
/// Computes consumed macros for a meal log. Macros come from the food the client actually ate — the
/// alternative food when one was substituted, otherwise the planned meal item's food — scaled by the
/// consumed grams (food macros are stored per 100g). Requires MealItem.Food and AlternativeFood loaded.
/// </summary>
public static class MealLogMacros
{
    public static MealLogDto ToDto(MealLog log)
    {
        var food = log.AlternativeFood ?? log.MealItem?.Food;
        var hasSnapshot = log.FoodCaloriesSnapshot.HasValue;
        var servingSize = log.FoodServingSizeSnapshot is > 0
            ? log.FoodServingSizeSnapshot.Value
            : (food?.ServingSize is > 0 ? food.ServingSize.Value : 100d);
        var factor = log.ConsumedQuantity / servingSize;

        return new MealLogDto
        {
            Id = log.Id,
            MealItemId = log.MealItemId,
            MealName = log.MealNameSnapshot ?? log.MealItem?.Meal?.Name ?? string.Empty,
            FoodName = log.FoodNameSnapshot ?? food?.Name ?? string.Empty,
            Unit = log.FoodUnitSnapshot ?? food?.ServingUnit,
            IsAlternative = log.AlternativeFoodId.HasValue,
            ConsumedQuantity = log.ConsumedQuantity,
            ConsumedAt = log.ConsumedAt,
            Calories = Math.Round((hasSnapshot ? log.FoodCaloriesSnapshot!.Value : food?.CaloriesPer100g ?? 0) * factor, 1),
            Protein = Math.Round((hasSnapshot ? log.FoodProteinSnapshot!.Value : food?.ProteinPer100g ?? 0) * factor, 1),
            Carbs = Math.Round((hasSnapshot ? log.FoodCarbsSnapshot!.Value : food?.CarbsPer100g ?? 0) * factor, 1),
            Fats = Math.Round((hasSnapshot ? log.FoodFatsSnapshot!.Value : food?.FatsPer100g ?? 0) * factor, 1)
        };
    }
}

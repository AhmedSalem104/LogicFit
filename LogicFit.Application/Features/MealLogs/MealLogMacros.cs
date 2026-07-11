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
        var food = log.AlternativeFood ?? log.MealItem.Food;
        var factor = log.ConsumedQuantity / 100.0;

        return new MealLogDto
        {
            Id = log.Id,
            MealItemId = log.MealItemId,
            FoodName = food.Name,
            IsAlternative = log.AlternativeFoodId.HasValue,
            ConsumedQuantity = log.ConsumedQuantity,
            ConsumedAt = log.ConsumedAt,
            Calories = Math.Round(food.CaloriesPer100g * factor, 1),
            Protein = Math.Round(food.ProteinPer100g * factor, 1),
            Carbs = Math.Round(food.CarbsPer100g * factor, 1),
            Fats = Math.Round(food.FatsPer100g * factor, 1)
        };
    }
}

using LogicFit.Application.Features.AthleteCheckins.DTOs;
using LogicFit.Application.Features.MealLogs;
using LogicFit.Domain.Entities;
using Xunit;

namespace LogicFit.Tests;

public sealed class TopGymMemberTrainingNutritionParityTests
{
    [Fact]
    public void Meal_log_macros_use_the_consumed_food_snapshot_and_serving_size()
    {
        var log = new MealLog
        {
            Id = Guid.NewGuid(),
            MealItemId = Guid.NewGuid(),
            ConsumedQuantity = 25,
            ConsumedAt = DateTime.UtcNow,
            MealNameSnapshot = "Breakfast",
            FoodNameSnapshot = "Oats",
            FoodUnitSnapshot = "g",
            FoodServingSizeSnapshot = 50,
            FoodCaloriesSnapshot = 200,
            FoodProteinSnapshot = 10,
            FoodCarbsSnapshot = 30,
            FoodFatsSnapshot = 5
        };

        var dto = MealLogMacros.ToDto(log);

        Assert.Equal("Breakfast", dto.MealName);
        Assert.Equal("Oats", dto.FoodName);
        Assert.Equal(100, dto.Calories);
        Assert.Equal(5, dto.Protein);
        Assert.Equal(15, dto.Carbs);
        Assert.Equal(2.5, dto.Fats);
    }

    [Fact]
    public void Athlete_checkin_readiness_is_a_coaching_indicator_between_zero_and_one_hundred()
    {
        var dto = new AthleteCheckinDto
        {
            SleepQuality = 5,
            Fatigue = 1,
            Soreness = 1,
            Stress = 1,
            Mood = 5
        };

        Assert.Equal(100, dto.ReadinessScore);
    }
}

namespace LogicFit.Application.Features.MealLogs.DTOs;

/// <summary>A day's consumed macros for a client, compared against their active diet-plan targets.</summary>
public class NutritionSummaryDto
{
    public DateTime Date { get; set; }
    public int LoggedCount { get; set; }

    public double ConsumedCalories { get; set; }
    public double ConsumedProtein { get; set; }
    public double ConsumedCarbs { get; set; }
    public double ConsumedFats { get; set; }

    public double TargetCalories { get; set; }
    public double TargetProtein { get; set; }
    public double TargetCarbs { get; set; }
    public double TargetFats { get; set; }
}

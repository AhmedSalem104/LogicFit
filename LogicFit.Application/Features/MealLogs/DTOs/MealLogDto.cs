namespace LogicFit.Application.Features.MealLogs.DTOs;

/// <summary>A meal the client logged as consumed, with macros computed from the eaten food.</summary>
public class MealLogDto
{
    public Guid Id { get; set; }
    public Guid MealItemId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public bool IsAlternative { get; set; }
    public double ConsumedQuantity { get; set; }
    public DateTime ConsumedAt { get; set; }
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fats { get; set; }
}

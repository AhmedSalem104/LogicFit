using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class MealLog : TenantAuditableEntity
{
    public Guid ClientId { get; set; }
    public Guid MealItemId { get; set; }
    public double ConsumedQuantity { get; set; }
    public DateTime ConsumedAt { get; set; }
    public int? AlternativeFoodId { get; set; }
    public string? MealNameSnapshot { get; set; }
    public string? FoodNameSnapshot { get; set; }
    public string? FoodUnitSnapshot { get; set; }
    public double? FoodServingSizeSnapshot { get; set; }
    public double? FoodCaloriesSnapshot { get; set; }
    public double? FoodProteinSnapshot { get; set; }
    public double? FoodCarbsSnapshot { get; set; }
    public double? FoodFatsSnapshot { get; set; }

    // Navigation Properties
    public virtual User Client { get; set; } = null!;
    public virtual MealItem MealItem { get; set; } = null!;
    public virtual Food? AlternativeFood { get; set; }
}

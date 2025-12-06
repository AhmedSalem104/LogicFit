using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class MealItem : TenantAuditableEntity
{
    public Guid MealId { get; set; }
    public int FoodId { get; set; }
    public double AssignedQuantity { get; set; }

    // Computed Fields (calculated in Application layer before saving)
    public double CalcCalories { get; set; }
    public double CalcProtein { get; set; }
    public double CalcCarbs { get; set; }
    public double CalcFats { get; set; }

    // Navigation Properties
    public virtual DailyMeal Meal { get; set; } = null!;
    public virtual Food Food { get; set; } = null!;
    public virtual ICollection<MealLog> Logs { get; set; } = new List<MealLog>();
}

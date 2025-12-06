using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class DailyMeal : TenantAuditableEntity
{
    public Guid PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OrderIndex { get; set; }

    // Navigation Properties
    public virtual DietPlan Plan { get; set; } = null!;
    public virtual ICollection<MealItem> Items { get; set; } = new List<MealItem>();
}

using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class Recipe : TenantAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public double TotalCalories { get; set; }
    public string? Description { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
}

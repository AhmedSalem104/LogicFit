using LogicFit.Domain.Common;
using LogicFit.Domain.Common.Interfaces;

namespace LogicFit.Domain.Entities;

public class Food : ISoftDeletable
{
    public int Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }

    // Macros (Fixed for performance)
    public double CaloriesPer100g { get; set; }
    public double ProteinPer100g { get; set; }
    public double CarbsPer100g { get; set; }
    public double FatsPer100g { get; set; }
    public double? FiberPer100g { get; set; }

    // Alternative Logic
    public string? AlternativeGroupId { get; set; }
    public bool IsVerified { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation Properties
    public virtual Tenant? Tenant { get; set; }
    public virtual ICollection<FoodMicronutrient> Micronutrients { get; set; } = new List<FoodMicronutrient>();
    public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public virtual ICollection<MealItem> MealItems { get; set; } = new List<MealItem>();
    public virtual ICollection<MealLog> AlternativeMealLogs { get; set; } = new List<MealLog>();
}

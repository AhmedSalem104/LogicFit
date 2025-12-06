using LogicFit.Domain.Common;
using LogicFit.Domain.Common.Interfaces;

namespace LogicFit.Domain.Entities;

public class NutrientDefinition : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public bool IsMicro { get; set; } = true;
    public double? DailyRecommended { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation Properties
    public virtual ICollection<FoodMicronutrient> FoodMicronutrients { get; set; } = new List<FoodMicronutrient>();
}

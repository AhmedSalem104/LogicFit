namespace LogicFit.Domain.Entities;

public class FoodMicronutrient
{
    public int FoodId { get; set; }
    public int NutrientId { get; set; }
    public double AmountPer100g { get; set; }

    // Navigation Properties
    public virtual Food Food { get; set; } = null!;
    public virtual NutrientDefinition Nutrient { get; set; } = null!;
}

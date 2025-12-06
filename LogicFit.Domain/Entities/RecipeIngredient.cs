namespace LogicFit.Domain.Entities;

public class RecipeIngredient
{
    public Guid RecipeId { get; set; }
    public int FoodId { get; set; }
    public double QuantityGrams { get; set; }

    // Navigation Properties
    public virtual Recipe Recipe { get; set; } = null!;
    public virtual Food Food { get; set; } = null!;
}

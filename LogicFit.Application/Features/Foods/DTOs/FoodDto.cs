namespace LogicFit.Application.Features.Foods.DTOs;

public class FoodDto
{
    public int Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public double CaloriesPer100g { get; set; }
    public double ProteinPer100g { get; set; }
    public double CarbsPer100g { get; set; }
    public double FatsPer100g { get; set; }
    public double? FiberPer100g { get; set; }
    public string? AlternativeGroupId { get; set; }
    public bool IsVerified { get; set; }
}

public class CreateFoodDto
{
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public double CaloriesPer100g { get; set; }
    public double ProteinPer100g { get; set; }
    public double CarbsPer100g { get; set; }
    public double FatsPer100g { get; set; }
    public double? FiberPer100g { get; set; }
    public string? AlternativeGroupId { get; set; }
}

public class UpdateFoodDto
{
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public double CaloriesPer100g { get; set; }
    public double ProteinPer100g { get; set; }
    public double CarbsPer100g { get; set; }
    public double FatsPer100g { get; set; }
    public double? FiberPer100g { get; set; }
    public string? AlternativeGroupId { get; set; }
}

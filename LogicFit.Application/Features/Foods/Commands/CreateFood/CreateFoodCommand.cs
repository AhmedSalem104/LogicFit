using MediatR;

namespace LogicFit.Application.Features.Foods.Commands.CreateFood;

public class CreateFoodCommand : IRequest<int>
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

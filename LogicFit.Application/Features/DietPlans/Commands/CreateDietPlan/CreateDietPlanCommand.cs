using MediatR;
using LogicFit.Application.Features.DietPlans.DTOs;
using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.DietPlans.Commands.CreateDietPlan;

public class CreateDietPlanCommand : IRequest<Guid>
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? MealsPerDay { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public double TargetCalories { get; set; }
    public double TargetProtein { get; set; }
    public double TargetCarbs { get; set; }
    public double TargetFats { get; set; }
    public string? CalorieGoal { get; set; }
    public double? CalorieAdjustment { get; set; }
    public string? CalculatorMetadata { get; set; }
    public string? Notes { get; set; }
    public PlanStatus? Status { get; set; }
    public List<DietMealInputDto> Meals { get; set; } = new();
}

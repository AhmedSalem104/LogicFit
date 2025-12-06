using MediatR;

namespace LogicFit.Application.Features.DietPlans.Commands.CreateDietPlan;

public class CreateDietPlanCommand : IRequest<Guid>
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public double TargetCalories { get; set; }
    public double TargetProtein { get; set; }
    public double TargetCarbs { get; set; }
    public double TargetFats { get; set; }
}

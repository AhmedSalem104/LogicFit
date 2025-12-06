using MediatR;

namespace LogicFit.Application.Features.DietPlans.Commands.CreateDailyMeal;

public class CreateDailyMealCommand : IRequest<Guid>
{
    public Guid PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

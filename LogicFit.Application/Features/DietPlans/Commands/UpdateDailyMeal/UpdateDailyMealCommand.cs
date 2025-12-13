using MediatR;

namespace LogicFit.Application.Features.DietPlans.Commands.UpdateDailyMeal;

public class UpdateDailyMealCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

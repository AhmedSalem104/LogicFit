using MediatR;

namespace LogicFit.Application.Features.DietPlans.Commands.DeleteDailyMeal;

public class DeleteDailyMealCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

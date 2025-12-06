using MediatR;

namespace LogicFit.Application.Features.DietPlans.Commands.DeleteDietPlan;

public class DeleteDietPlanCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

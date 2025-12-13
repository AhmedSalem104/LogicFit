using MediatR;

namespace LogicFit.Application.Features.DietPlans.Commands.DuplicateDietPlan;

public class DuplicateDietPlanCommand : IRequest<Guid>
{
    public Guid Id { get; set; }
    public Guid? NewClientId { get; set; }
    public string? NewName { get; set; }
}

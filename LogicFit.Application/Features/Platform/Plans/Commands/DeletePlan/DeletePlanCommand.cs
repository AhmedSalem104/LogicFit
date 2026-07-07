using MediatR;

namespace LogicFit.Application.Features.Platform.Plans.Commands.DeletePlan;

public class DeletePlanCommand : IRequest<Unit>
{
    public Guid Id { get; set; }

    public DeletePlanCommand(Guid id) => Id = id;
}

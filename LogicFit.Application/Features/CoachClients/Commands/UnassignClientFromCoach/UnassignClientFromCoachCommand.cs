using MediatR;

namespace LogicFit.Application.Features.CoachClients.Commands.UnassignClientFromCoach;

public class UnassignClientFromCoachCommand : IRequest<Unit>
{
    public Guid ClientId { get; set; }
}

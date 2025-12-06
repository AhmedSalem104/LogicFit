using MediatR;

namespace LogicFit.Application.Features.WorkoutSessions.Commands.EndWorkoutSession;

public class EndWorkoutSessionCommand : IRequest<bool>
{
    public Guid SessionId { get; set; }
    public string? Notes { get; set; }
}

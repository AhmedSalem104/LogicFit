using MediatR;

namespace LogicFit.Application.Features.WorkoutSessions.Commands.StartWorkoutSession;

public class StartWorkoutSessionCommand : IRequest<Guid>
{
    public Guid RoutineId { get; set; }
}

using MediatR;

namespace LogicFit.Application.Features.WorkoutSessions.Commands.CreateSessionSet;

public class CreateSessionSetCommand : IRequest<Guid>
{
    public Guid SessionId { get; set; }
    public int ExerciseId { get; set; }
    public int SetNumber { get; set; }
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public double? Rpe { get; set; }
}

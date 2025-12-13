using MediatR;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.DeleteRoutineExercise;

public class DeleteRoutineExerciseCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

using MediatR;

namespace LogicFit.Application.Features.Exercises.Commands.DeleteExercise;

public class DeleteExerciseCommand : IRequest<bool>
{
    public int Id { get; set; }
}

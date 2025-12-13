using MediatR;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.UpdateRoutineExercise;

public class UpdateRoutineExerciseCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public int? ExerciseId { get; set; }
    public int Sets { get; set; }
    public int RepsMin { get; set; }
    public int RepsMax { get; set; }
    public int RestSec { get; set; }
    public Guid? SupersetGroupId { get; set; }
}

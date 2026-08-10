using FluentValidation;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.CreateRoutineExercise;

public sealed class CreateRoutineExerciseCommandValidator : AbstractValidator<CreateRoutineExerciseCommand>
{
    public CreateRoutineExerciseCommandValidator()
    {
        RuleFor(x => x.RoutineId).NotEmpty();
        RuleFor(x => x.ExerciseId).GreaterThan(0);
        RuleFor(x => x.Sets).GreaterThan(0);
        RuleFor(x => x.RepsMin).GreaterThan(0);
        RuleFor(x => x.RepsMax).GreaterThanOrEqualTo(x => x.RepsMin);
        RuleFor(x => x.RestSec).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TargetWeightKg).GreaterThanOrEqualTo(0).When(x => x.TargetWeightKg.HasValue);
    }
}

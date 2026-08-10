using FluentValidation;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.CreateWorkoutProgram;

public sealed class CreateWorkoutProgramCommandValidator : AbstractValidator<CreateWorkoutProgramCommand>
{
    public CreateWorkoutProgramCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).When(x => x.EndDate.HasValue);
        RuleFor(x => x.DaysPerWeek).InclusiveBetween(1, 7).When(x => x.DaysPerWeek.HasValue);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Routines).NotEmpty().WithMessage("At least one workout day is required.");

        RuleForEach(x => x.Routines).ChildRules(routine =>
        {
            routine.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            routine.RuleFor(x => x.DayOfWeek).InclusiveBetween(0, 6);
            routine.RuleFor(x => x.Exercises).NotEmpty().WithMessage("Each workout day needs an exercise.");
            routine.RuleForEach(x => x.Exercises).ChildRules(exercise =>
            {
                exercise.RuleFor(x => x.ExerciseId).GreaterThan(0);
                exercise.RuleFor(x => x.Sets).GreaterThan(0);
                exercise.RuleFor(x => x.RepsMin).GreaterThan(0);
                exercise.RuleFor(x => x.RepsMax).GreaterThanOrEqualTo(x => x.RepsMin);
                exercise.RuleFor(x => x.RestSec).GreaterThanOrEqualTo(0);
                exercise.RuleFor(x => x.TargetWeightKg).GreaterThanOrEqualTo(0).When(x => x.TargetWeightKg.HasValue);
            });
        });
    }
}

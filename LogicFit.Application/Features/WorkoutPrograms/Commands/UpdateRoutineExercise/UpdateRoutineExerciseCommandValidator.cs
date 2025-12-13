using FluentValidation;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.UpdateRoutineExercise;

public class UpdateRoutineExerciseCommandValidator : AbstractValidator<UpdateRoutineExerciseCommand>
{
    public UpdateRoutineExerciseCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Exercise Id is required");

        RuleFor(x => x.Sets)
            .GreaterThan(0).WithMessage("Sets must be greater than 0");

        RuleFor(x => x.RepsMin)
            .GreaterThan(0).WithMessage("Minimum reps must be greater than 0");

        RuleFor(x => x.RepsMax)
            .GreaterThanOrEqualTo(x => x.RepsMin).WithMessage("Maximum reps must be greater than or equal to minimum reps");

        RuleFor(x => x.RestSec)
            .GreaterThanOrEqualTo(0).WithMessage("Rest seconds must be non-negative");
    }
}

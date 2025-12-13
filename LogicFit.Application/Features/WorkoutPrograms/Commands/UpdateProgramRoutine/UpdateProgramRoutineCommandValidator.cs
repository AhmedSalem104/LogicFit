using FluentValidation;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.UpdateProgramRoutine;

public class UpdateProgramRoutineCommandValidator : AbstractValidator<UpdateProgramRoutineCommand>
{
    public UpdateProgramRoutineCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Routine Id is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.DayOfWeek)
            .InclusiveBetween(0, 6).WithMessage("Day of week must be between 0 (Sunday) and 6 (Saturday)");
    }
}

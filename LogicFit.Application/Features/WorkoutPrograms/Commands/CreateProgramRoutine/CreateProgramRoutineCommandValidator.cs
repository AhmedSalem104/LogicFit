using FluentValidation;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.CreateProgramRoutine;

public sealed class CreateProgramRoutineCommandValidator : AbstractValidator<CreateProgramRoutineCommand>
{
    public CreateProgramRoutineCommandValidator()
    {
        RuleFor(x => x.ProgramId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DayOfWeek).InclusiveBetween(0, 6);
    }
}

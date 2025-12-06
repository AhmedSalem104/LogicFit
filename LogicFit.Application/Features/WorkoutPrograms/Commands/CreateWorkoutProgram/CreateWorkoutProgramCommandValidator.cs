using FluentValidation;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.CreateWorkoutProgram;

public class CreateWorkoutProgramCommandValidator : AbstractValidator<CreateWorkoutProgramCommand>
{
    public CreateWorkoutProgramCommandValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("Client is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be after start date");
    }
}

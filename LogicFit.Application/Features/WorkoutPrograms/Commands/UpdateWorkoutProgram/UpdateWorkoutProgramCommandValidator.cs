using FluentValidation;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.UpdateWorkoutProgram;

public class UpdateWorkoutProgramCommandValidator : AbstractValidator<UpdateWorkoutProgramCommand>
{
    public UpdateWorkoutProgramCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Program Id is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");
    }
}

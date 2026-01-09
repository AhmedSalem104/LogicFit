using FluentValidation;

namespace LogicFit.Application.Features.Muscles.Commands.UpdateMuscle;

public class UpdateMuscleCommandValidator : AbstractValidator<UpdateMuscleCommand>
{
    public UpdateMuscleCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Invalid muscle ID");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Muscle name is required")
            .MaximumLength(100).WithMessage("Muscle name must not exceed 100 characters");

        RuleFor(x => x.NameAr)
            .MaximumLength(100).WithMessage("Arabic muscle name must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.NameAr));

        RuleFor(x => x.BodyPart)
            .MaximumLength(50).WithMessage("Body part must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.BodyPart));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.DescriptionAr)
            .MaximumLength(500).WithMessage("Arabic description must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.DescriptionAr));

        RuleFor(x => x.Icon)
            .MaximumLength(200).WithMessage("Icon URL must not exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Icon));
    }
}

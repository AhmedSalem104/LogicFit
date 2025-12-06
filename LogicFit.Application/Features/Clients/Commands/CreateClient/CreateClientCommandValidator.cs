using FluentValidation;

namespace LogicFit.Application.Features.Clients.Commands.CreateClient;

public class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^[\d\+\-\s]+$").WithMessage("Invalid phone number format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.FullName)
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.FullName));
    }
}

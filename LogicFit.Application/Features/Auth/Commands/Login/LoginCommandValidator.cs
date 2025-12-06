using FluentValidation;

namespace LogicFit.Application.Features.Auth.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^[\d\+\-\s]+$").WithMessage("Invalid phone number format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required");
    }
}

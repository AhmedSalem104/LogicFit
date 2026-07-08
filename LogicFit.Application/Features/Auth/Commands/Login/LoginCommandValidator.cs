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

        // Identify the gym by subdomain (preferred) OR tenantId.
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Subdomain) || x.TenantId != Guid.Empty)
            .WithName("subdomain")
            .WithMessage("Provide the gym 'subdomain' (or 'tenantId').");
    }
}

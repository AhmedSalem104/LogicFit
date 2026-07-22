using FluentValidation;

namespace LogicFit.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.");

        RuleFor(x => x.ResetToken)
            .NotEmpty().WithMessage("Reset token is required.")
            .Length(6).WithMessage("Reset token must be 6 digits.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        // Identify the gym by subdomain (preferred) OR tenantId.
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Subdomain) || x.TenantId != Guid.Empty)
            .WithName("subdomain")
            .WithMessage("Provide the gym 'subdomain' (or 'tenantId').");
    }
}

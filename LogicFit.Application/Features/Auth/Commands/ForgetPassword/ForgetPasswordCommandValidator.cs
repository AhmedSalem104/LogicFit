using FluentValidation;

namespace LogicFit.Application.Features.Auth.Commands.ForgetPassword;

public class ForgetPasswordCommandValidator : AbstractValidator<ForgetPasswordCommand>
{
    public ForgetPasswordCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.");

        // Identify the gym by subdomain (preferred) OR tenantId.
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Subdomain) || x.TenantId != Guid.Empty)
            .WithName("subdomain")
            .WithMessage("Provide the gym 'subdomain' (or 'tenantId').");
    }
}

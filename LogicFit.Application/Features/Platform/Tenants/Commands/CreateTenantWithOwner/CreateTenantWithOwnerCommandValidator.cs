using FluentValidation;

namespace LogicFit.Application.Features.Platform.Tenants.Commands.CreateTenantWithOwner;

public sealed class CreateTenantWithOwnerCommandValidator : AbstractValidator<CreateTenantWithOwnerCommand>
{
    public CreateTenantWithOwnerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(x => x.Subdomain)
            .Matches("^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$")
            .When(x => !string.IsNullOrWhiteSpace(x.Subdomain));
        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.PhoneNumber)
            .MaximumLength(32)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        RuleFor(x => x.OwnerEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
        RuleFor(x => x.OwnerPhoneNumber)
            .MaximumLength(32)
            .When(x => !string.IsNullOrWhiteSpace(x.OwnerPhoneNumber));
        RuleFor(x => x.OwnerPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);
        RuleFor(x => x.OwnerFullName)
            .NotEmpty()
            .MaximumLength(200);
    }
}

using FluentValidation;
using LogicFit.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Tenants.Commands.CreateTenant;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateTenantCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Subdomain)
            .NotEmpty().WithMessage("Subdomain is required")
            .MaximumLength(100).WithMessage("Subdomain must not exceed 100 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Subdomain can only contain lowercase letters, numbers, and hyphens")
            .MustAsync(BeUniqueSubdomain).WithMessage("Subdomain already exists");

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage("Logo URL must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.LogoUrl));

        RuleFor(x => x.PrimaryColor)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Primary color must be a valid hex color")
            .When(x => !string.IsNullOrEmpty(x.PrimaryColor));

        RuleFor(x => x.SecondaryColor)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Secondary color must be a valid hex color")
            .When(x => !string.IsNullOrEmpty(x.SecondaryColor));
    }

    private async Task<bool> BeUniqueSubdomain(string subdomain, CancellationToken cancellationToken)
    {
        return !await _context.Tenants
            .AnyAsync(t => t.Subdomain != null && t.Subdomain.ToLower() == subdomain.ToLower(), cancellationToken);
    }
}

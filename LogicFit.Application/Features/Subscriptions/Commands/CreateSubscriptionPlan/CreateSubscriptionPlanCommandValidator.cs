using FluentValidation;
using LogicFit.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;

public class CreateSubscriptionPlanCommandValidator : AbstractValidator<CreateSubscriptionPlanCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateSubscriptionPlanCommandValidator(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters")
            .MustAsync(BeUniqueName).WithMessage("A plan with this name already exists");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0");

        RuleFor(x => x.DurationMonths)
            .GreaterThan(0).WithMessage("Duration must be greater than 0");

        RuleFor(x => x.MaxFreezeDays)
            .GreaterThanOrEqualTo(0).WithMessage("Max freeze days cannot be negative");

        RuleFor(x => x.MaxFreezeCount)
            .GreaterThanOrEqualTo(0).WithMessage("Max freeze count cannot be negative");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters")
            .When(x => x.Description != null);
    }

    private async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        return !await _context.SubscriptionPlans
            .AnyAsync(p => p.TenantId == tenantId && p.Name == name, cancellationToken);
    }
}

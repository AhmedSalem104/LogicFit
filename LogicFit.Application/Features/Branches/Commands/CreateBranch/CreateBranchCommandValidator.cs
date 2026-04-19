using FluentValidation;
using LogicFit.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Branches.Commands.CreateBranch;

public class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateBranchCommandValidator(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200);

        RuleFor(x => x.Code)
            .MaximumLength(50)
            .MustAsync(BeUniqueCode).WithMessage("A branch with this code already exists")
            .When(x => !string.IsNullOrEmpty(x.Code));

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Capacity)
            .GreaterThan(0).When(x => x.Capacity.HasValue);
    }

    private async Task<bool> BeUniqueCode(string? code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(code)) return true;
        var tenantId = _tenantService.GetCurrentTenantId();
        return !await _context.Branches.AnyAsync(b => b.TenantId == tenantId && b.Code == code, cancellationToken);
    }
}

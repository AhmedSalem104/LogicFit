using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Common.Services;

/// <summary>
/// Default <see cref="ICommissionService"/>. Maps the selling user to their EmployeeProfile, finds the
/// best-matching active CommissionRule for the sale's source type, computes the amount, and stages a
/// pending Commission. Only employees (users that have an EmployeeProfile) can earn commissions, which
/// matches the data model (Commission.EmployeeId -> EmployeeProfile). Role-based rules match on the
/// employee's user role.
/// </summary>
public class CommissionService : ICommissionService
{
    private readonly IApplicationDbContext _context;

    public CommissionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> AccrueAsync(
        Guid tenantId,
        Guid? sellerUserId,
        CommissionSourceType sourceType,
        decimal sourceAmount,
        Guid referenceId,
        DateTime earnedDate,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (sellerUserId is null || sellerUserId == Guid.Empty || sourceAmount <= 0)
            return 0m;

        // Only employees earn commissions.
        var employee = await _context.EmployeeProfiles
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.UserId == sellerUserId.Value, cancellationToken);
        if (employee is null)
            return 0m;

        // Seller's role, for role-based rules.
        var sellerRole = await _context.Users
            .Where(u => u.Id == sellerUserId.Value)
            .Select(u => (UserRole?)u.Role)
            .FirstOrDefaultAsync(cancellationToken);

        var candidateRules = await _context.CommissionRules
            .Where(r => r.TenantId == tenantId
                        && r.IsActive
                        && r.SourceType == sourceType
                        && (r.EmployeeId == employee.Id
                            || (r.EmployeeId == null && r.Role != null && r.Role == sellerRole)))
            .ToListAsync(cancellationToken);
        if (candidateRules.Count == 0)
            return 0m;

        // Respect the rule's minimum sale amount, then prefer an employee-specific rule over a role rule.
        var rule = candidateRules
            .Where(r => r.MinAmount == null || sourceAmount >= r.MinAmount.Value)
            .OrderByDescending(r => r.EmployeeId == employee.Id)
            .ThenByDescending(r => r.Value)
            .FirstOrDefault();
        if (rule is null)
            return 0m;

        var amount = rule.Type == CommissionRuleType.Percentage
            ? Math.Round(sourceAmount * (rule.Value / 100m), 2, MidpointRounding.AwayFromZero)
            : rule.Value;

        if (amount <= 0)
            return 0m;

        _context.Commissions.Add(new Commission
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employee.Id,
            SourceType = sourceType,
            ReferenceId = referenceId,
            Amount = amount,
            SourceAmount = sourceAmount,
            EarnedDate = earnedDate,
            Status = CommissionStatus.Pending,
            Description = description
        });

        return amount;
    }
}

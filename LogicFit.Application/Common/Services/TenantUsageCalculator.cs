using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Common.Services;

public class TenantUsageCalculator : ITenantUsageCalculator
{
    private readonly IApplicationDbContext _context;

    public TenantUsageCalculator(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TenantUsageSnapshot> CalculateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var members = await _context.Users.IgnoreQueryFilters()
            .CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Client && !u.IsDeleted, cancellationToken);
        var coaches = await _context.Users.IgnoreQueryFilters()
            .CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Coach && !u.IsDeleted, cancellationToken);
        var branches = await _context.Branches.IgnoreQueryFilters()
            .CountAsync(b => b.TenantId == tenantId && !b.IsDeleted, cancellationToken);
        var employees = await _context.EmployeeProfiles.IgnoreQueryFilters()
            .CountAsync(e => e.TenantId == tenantId && !e.IsDeleted, cancellationToken);

        return new TenantUsageSnapshot(members, coaches, branches, employees);
    }
}

using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LogicFit.Infrastructure.Security;

/// <summary>Database-backed implementation of the identity/membership access boundary.</summary>
public sealed class IdentityWorkspaceAccessGuard : IIdentityWorkspaceAccessGuard
{
    private readonly IApplicationDbContext _context;
    private readonly IdentityAccessOptions _options;

    public IdentityWorkspaceAccessGuard(
        IApplicationDbContext context,
        IOptions<IdentityAccessOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task<IdentityWorkspaceAccessDecision> EvaluateAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .Where(x => x.Id == userId && x.TenantId == workspaceId && !x.IsDeleted)
            .Select(x => new { x.IsActive, x.IdentityAccountId })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return IdentityWorkspaceAccessPolicy.Evaluate(
                new IdentityWorkspaceAccessState(false, false, false, false, false),
                _options.AllowUnlinkedLegacySessions);
        }

        if (!user.IdentityAccountId.HasValue)
        {
            return IdentityWorkspaceAccessPolicy.Evaluate(
                new IdentityWorkspaceAccessState(true, user.IsActive, false, false, false),
                _options.AllowUnlinkedLegacySessions);
        }

        var identityIsActive = await _context.IdentityAccounts
            .IgnoreQueryFilters()
            .Where(x => x.Id == user.IdentityAccountId.Value)
            .Select(x => (bool?)x.IsActive)
            .FirstOrDefaultAsync(cancellationToken) == true;

        var membershipIsActive = await _context.WorkspaceMemberships
            .IgnoreQueryFilters()
            .AnyAsync(x => x.UserId == userId &&
                           x.IdentityAccountId == user.IdentityAccountId.Value &&
                           x.TenantId == workspaceId &&
                           x.Status == Domain.Enums.WorkspaceMembershipStatus.Active &&
                           !x.IsDeleted,
                cancellationToken);

        return IdentityWorkspaceAccessPolicy.Evaluate(
            new IdentityWorkspaceAccessState(true, user.IsActive, true, identityIsActive, membershipIsActive),
            _options.AllowUnlinkedLegacySessions);
    }
}

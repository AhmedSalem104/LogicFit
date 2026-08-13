using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Common.Services;

public class RbacService : IRbacService
{
    private readonly IApplicationDbContext _context;

    public RbacService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserAuthorization> GetUserAuthorizationAsync(Guid userId, CancellationToken cancellationToken = default)
        => await GetUserAuthorizationCoreAsync(userId, tenantId: null, cancellationToken);

    public async Task<UserAuthorization> GetUserAuthorizationForTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
        => await GetUserAuthorizationCoreAsync(userId, tenantId, cancellationToken);

    private async Task<UserAuthorization> GetUserAuthorizationCoreAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters: role resolution runs during anonymous login before a tenant is set.
        var roleAssignments = _context.UserRoleAssignments
            .IgnoreQueryFilters()
            .Where(ur => ur.UserId == userId);

        if (tenantId.HasValue)
            roleAssignments = roleAssignments.Where(ur => ur.TenantId == tenantId.Value);

        var roleIds = await roleAssignments
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
        {
            return new UserAuthorization(Array.Empty<string>(), Array.Empty<string>());
        }

        var roles = await _context.AppRoles
            .IgnoreQueryFilters()
            .Where(r => roleIds.Contains(r.Id) && !r.IsDeleted)
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);

        var permissions = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new UserAuthorization(roles, permissions);
    }

    public async Task EnsureUserInRoleAsync(Guid userId, Guid? tenantId, string systemRoleName, CancellationToken cancellationToken = default)
    {
        var role = await _context.AppRoles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.TenantId == null && r.Name == systemRoleName && !r.IsDeleted, cancellationToken);

        if (role == null)
        {
            throw new InvalidOperationException($"System role '{systemRoleName}' is not seeded.");
        }

        var alreadyAssigned = await _context.UserRoleAssignments
            .IgnoreQueryFilters()
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == role.Id, cancellationToken);

        if (!alreadyAssigned)
        {
            _context.UserRoleAssignments.Add(new UserRoleAssignment
            {
                UserId = userId,
                RoleId = role.Id,
                TenantId = tenantId
            });
        }
    }
}

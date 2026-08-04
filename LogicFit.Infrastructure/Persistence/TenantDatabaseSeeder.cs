using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Seeds the local reference/RBAC projection after a tenant database is allocated. The platform
/// owns the catalog definitions, but each tenant database needs its own copy because operational
/// queries must never cross a database boundary.
/// </summary>
public sealed class TenantDatabaseSeeder(
    ILogger<TenantDatabaseSeeder> logger,
    TenantReferenceCatalogSeeder referenceCatalogSeeder)
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RolePermissions =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [SystemRoles.Owner] = Permissions.TenantPermissions,
            [SystemRoles.Manager] = Permissions.TenantPermissions
                .Where(permission => permission != Permissions.ManageSettings && permission != Permissions.ManageTenantBilling)
                .ToArray(),
            [SystemRoles.Receptionist] = new[]
            {
                Permissions.ViewMembers, Permissions.ManageMembers, Permissions.CreateMembers,
                Permissions.UpdateMembers, Permissions.DeleteMembers, Permissions.ManageAttendance,
                Permissions.ManageClientSubscriptions, Permissions.ManagePOS
            },
            [SystemRoles.Accountant] = new[]
            {
                Permissions.ManageFinance, Permissions.ViewReports, Permissions.ManageReports,
                Permissions.ManageTenantBilling
            },
            [SystemRoles.Coach] = new[]
            {
                Permissions.ViewMembers, Permissions.ManageAttendance, Permissions.ViewReports
            },
            [SystemRoles.Trainer] = new[]
            {
                Permissions.ViewMembers, Permissions.ManageAttendance, Permissions.ViewReports
            },
            [SystemRoles.FreelanceOwner] = Permissions.TenantPermissions,
            [SystemRoles.FreelanceCoach] = new[]
            {
                Permissions.ViewMembers, Permissions.CreateMembers, Permissions.UpdateMembers,
                Permissions.ManageCoaches, Permissions.ManageAttendance,
                Permissions.ManageClientSubscriptions, Permissions.ViewReports
            },
            [SystemRoles.FreelanceAssistant] = new[]
            {
                Permissions.ViewMembers, Permissions.CreateMembers, Permissions.UpdateMembers,
                Permissions.ManageAttendance, Permissions.ManageClientSubscriptions
            },
            [SystemRoles.Client] = Array.Empty<string>()
        };

    public async Task SeedAsync(TenantDbContext context, CancellationToken cancellationToken = default)
    {
        await referenceCatalogSeeder.SeedAsync(context, cancellationToken);
        await SeedPermissionsAsync(context, cancellationToken);
        await SeedRolesAsync(context, cancellationToken);
        logger.LogInformation("Tenant reference and RBAC seed completed for TenantId {TenantId}.", context.TenantId);
    }

    private static async Task SeedPermissionsAsync(
        TenantDbContext context,
        CancellationToken cancellationToken)
    {
        var existing = await context.Permissions
            .IgnoreQueryFilters()
            .ToDictionaryAsync(permission => permission.Code, StringComparer.Ordinal, cancellationToken);

        foreach (var code in Permissions.TenantPermissions)
        {
            if (existing.ContainsKey(code))
                continue;

            var permission = new Permission
            {
                Code = code,
                DisplayName = code,
                DisplayNameAr = code,
                Category = "Tenant",
                IsPlatformPermission = false
            };
            context.Permissions.Add(permission);
            existing[code] = permission;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolesAsync(
        TenantDbContext context,
        CancellationToken cancellationToken)
    {
        var permissions = await context.Permissions
            .IgnoreQueryFilters()
            .Where(permission => Permissions.TenantPermissions.Contains(permission.Code))
            .ToDictionaryAsync(permission => permission.Code, StringComparer.Ordinal, cancellationToken);
        var roles = await context.AppRoles
            .IgnoreQueryFilters()
            .Where(role => role.TenantId == null && RolePermissions.Keys.Contains(role.Name) && !role.IsDeleted)
            .ToDictionaryAsync(role => role.Name, StringComparer.Ordinal, cancellationToken);

        foreach (var roleName in RolePermissions.Keys)
        {
            if (roles.ContainsKey(roleName))
                continue;

            var role = new Role
            {
                Name = roleName,
                NameAr = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                Description = $"System role: {roleName}",
                IsSystemRole = true
            };
            context.AppRoles.Add(role);
            roles[roleName] = role;
        }

        await context.SaveChangesAsync(cancellationToken);

        var roleIds = roles.Values.Select(role => role.Id).ToArray();
        var existingMappings = await context.RolePermissions
            .Where(mapping => roleIds.Contains(mapping.RoleId))
            .Select(mapping => new { mapping.RoleId, mapping.PermissionId })
            .ToListAsync(cancellationToken);
        var existingMappingKeys = existingMappings
            .Select(mapping => (mapping.RoleId, mapping.PermissionId))
            .ToHashSet();

        foreach (var (roleName, permissionCodes) in RolePermissions)
        {
            var role = roles[roleName];
            foreach (var permissionCode in permissionCodes)
            {
                if (!permissions.TryGetValue(permissionCode, out var permission) ||
                    !existingMappingKeys.Add((role.Id, permission.Id)))
                    continue;

                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Seeds the RBAC reference data (permissions, system roles, role-permission maps) and
/// backfills UserRole assignments for users that predate the RBAC tables. Idempotent.
/// </summary>
public class RbacSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RbacSeeder> _logger;

    public RbacSeeder(ApplicationDbContext context, ILogger<RbacSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Default system-role -> permission-code mapping.
    private static readonly Dictionary<string, string[]> RolePermissionMap = new()
    {
        [SystemRoles.Owner] = Permissions.TenantPermissions.ToArray(),
        [SystemRoles.Manager] = Permissions.TenantPermissions
            .Where(p => p != Permissions.ManageSettings && p != Permissions.ManageTenantBilling)
            .ToArray(),
        [SystemRoles.Receptionist] = new[]
        {
            Permissions.ViewMembers, Permissions.ManageMembers, Permissions.ManageAttendance,
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
        [SystemRoles.Client] = Array.Empty<string>(),
        [SystemRoles.PlatformOwner] = Permissions.PlatformPermissions.ToArray(),
        [SystemRoles.PlatformAdmin] = new[]
        {
            Permissions.ManageTenants, Permissions.ManagePlans,
            Permissions.ManagePaymentRequests, Permissions.ManagePlatformReports
        }
    };

    // Legacy UserRole enum -> system role name (for backfill).
    private static readonly Dictionary<UserRole, string> LegacyRoleMap = new()
    {
        [UserRole.Owner] = SystemRoles.Owner,
        [UserRole.Coach] = SystemRoles.Coach,
        [UserRole.Client] = SystemRoles.Client,
        [UserRole.Manager] = SystemRoles.Manager,
        [UserRole.Receptionist] = SystemRoles.Receptionist,
        [UserRole.Accountant] = SystemRoles.Accountant,
        [UserRole.Trainer] = SystemRoles.Trainer,
        [UserRole.PlatformOwner] = SystemRoles.PlatformOwner,
        [UserRole.PlatformAdmin] = SystemRoles.PlatformAdmin
    };

    public async Task SeedAsync()
    {
        await SeedPermissionsAsync();
        await SeedRolesAndMappingsAsync();
        await SeedPlatformAsync();
        await BackfillUserRolesAsync();
        _logger.LogInformation("RBAC seeding completed");
    }

    private async Task SeedPlatformAsync()
    {
        // Sentinel platform tenant (owns platform users; satisfies the User->Tenant FK).
        var platformTenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == PlatformConstants.PlatformTenantId);

        if (platformTenant == null)
        {
            _context.Tenants.Add(new Tenant
            {
                Id = PlatformConstants.PlatformTenantId,
                Name = "Platform",
                Subdomain = null,
                Status = TenantStatus.Active
            });
            await _context.SaveChangesAsync();
        }

        // Default platform owner (bootstrap). Backfill assigns the PlatformOwner role afterwards.
        var ownerExists = await _context.Set<User>()
            .IgnoreQueryFilters()
            .AnyAsync(u => u.TenantId == PlatformConstants.PlatformTenantId && u.Role == UserRole.PlatformOwner);

        if (!ownerExists)
        {
            var owner = new User
            {
                TenantId = PlatformConstants.PlatformTenantId,
                Email = "owner@platform.local",
                PhoneNumber = null,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe#12345"),
                Role = UserRole.PlatformOwner,
                IsActive = true
            };
            _context.Set<User>().Add(owner);
            _context.UserProfiles.Add(new UserProfile { UserId = owner.Id, FullName = "Platform Owner" });
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Seeded default PlatformOwner (owner@platform.local / ChangeMe#12345). CHANGE THIS PASSWORD IMMEDIATELY.");
        }
    }

    private async Task SeedPermissionsAsync()
    {
        var existingCodes = await _context.Permissions.Select(p => p.Code).ToListAsync();
        var missing = Permissions.All.Where(code => !existingCodes.Contains(code)).ToList();

        foreach (var code in missing)
        {
            _context.Permissions.Add(new Permission
            {
                Code = code,
                DisplayName = code,
                Category = Permissions.PlatformPermissions.Contains(code) ? "Platform" : "Tenant",
                IsPlatformPermission = Permissions.PlatformPermissions.Contains(code)
            });
        }

        if (missing.Count > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} permissions", missing.Count);
        }
    }

    private async Task SeedRolesAndMappingsAsync()
    {
        var permissionsByCode = await _context.Permissions.ToDictionaryAsync(p => p.Code, p => p.Id);

        foreach (var (roleName, permissionCodes) in RolePermissionMap)
        {
            var role = await _context.AppRoles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.TenantId == null && r.Name == roleName);

            if (role == null)
            {
                role = new Role
                {
                    TenantId = null,
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    Description = $"System role: {roleName}",
                    IsSystemRole = true
                };
                _context.AppRoles.Add(role);
                await _context.SaveChangesAsync();
            }

            // Ensure the role has exactly its mapped permissions (add any missing).
            var existingPermIds = await _context.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            foreach (var code in permissionCodes)
            {
                if (!permissionsByCode.TryGetValue(code, out var permId)) continue;
                if (existingPermIds.Contains(permId)) continue;

                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permId
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task BackfillUserRolesAsync()
    {
        // Users that have no UserRole assignment yet.
        var assignedUserIds = await _context.UserRoleAssignments
            .IgnoreQueryFilters()
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync();

        var users = await _context.Set<User>()
            .IgnoreQueryFilters()
            .Where(u => !assignedUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.TenantId, u.Role })
            .ToListAsync();

        if (users.Count == 0) return;

        var systemRoles = await _context.AppRoles
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == null)
            .ToDictionaryAsync(r => r.Name, r => r.Id);

        var added = 0;
        foreach (var user in users)
        {
            if (!LegacyRoleMap.TryGetValue(user.Role, out var roleName)) continue;
            if (!systemRoles.TryGetValue(roleName, out var roleId)) continue;

            _context.UserRoleAssignments.Add(new UserRoleAssignment
            {
                UserId = user.Id,
                RoleId = roleId,
                TenantId = user.TenantId
            });
            added++;
        }

        if (added > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Backfilled {Count} user-role assignments", added);
        }
    }
}

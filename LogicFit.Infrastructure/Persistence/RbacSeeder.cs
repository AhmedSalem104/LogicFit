using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Application.Features.Identity;
using LogicFit.Application.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Seeds the RBAC reference data (permissions, system roles, role-permission maps) and
/// backfills UserRole assignments for users that predate the RBAC tables. Idempotent.
/// </summary>
public class RbacSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RbacSeeder> _logger;
    private readonly PlatformOwnerBootstrapOptions _bootstrap;

    public RbacSeeder(
        ApplicationDbContext context,
        ILogger<RbacSeeder> logger,
        IOptions<PlatformOwnerBootstrapOptions> bootstrap)
    {
        _context = context;
        _logger = logger;
        _bootstrap = bootstrap.Value;
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
            Permissions.ViewMembers, Permissions.ManageMembers, Permissions.CreateMembers, Permissions.UpdateMembers, Permissions.DeleteMembers, Permissions.ManageAttendance,
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
        // Freelance workspaces manage clients, coaching delivery and their own
        // finance. Gym infrastructure is deliberately not part of this role.
        [SystemRoles.FreelanceOwner] = new[]
        {
            Permissions.ViewMembers, Permissions.ManageMembers, Permissions.CreateMembers,
            Permissions.UpdateMembers, Permissions.DeleteMembers, Permissions.ManageCoaches,
            Permissions.ManageClientSubscriptions,
            Permissions.ManageFinance, Permissions.ViewReports, Permissions.ManageReports,
            Permissions.ManageSettings, Permissions.ManageTenantBilling,
            Permissions.CreateAndDownloadTenantBackup
        },
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
        [UserRole.FreelanceOwner] = SystemRoles.FreelanceOwner,
        [UserRole.FreelanceCoach] = SystemRoles.FreelanceCoach,
        [UserRole.FreelanceAssistant] = SystemRoles.FreelanceAssistant,
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

        var owner = await _context.Set<User>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(u =>
                u.TenantId == PlatformConstants.PlatformTenantId && u.Role == UserRole.PlatformOwner);

        if (!_bootstrap.Enabled)
        {
            if (owner is null || !owner.IdentityAccountId.HasValue)
                _logger.LogWarning("Platform Owner login is not ready. Use the protected PlatformBootstrap server settings for one recovery run.");
            return;
        }

        PlatformOwnerBootstrapOptions.Validate(_bootstrap);
        var normalizedEmail = _bootstrap.GetNormalizedEmail();
        var normalizedPhone = _bootstrap.GetNormalizedPhoneNumber();
        var now = DateTime.UtcNow;

        var linkedIdentity = owner?.IdentityAccountId is Guid identityId
            ? await _context.IdentityAccounts.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == identityId)
            : null;
        if (owner?.IdentityAccountId.HasValue == true && linkedIdentity is null)
            throw new InvalidOperationException("The Platform Owner references a missing IdentityAccount.");

        var emailIdentity = await _context.IdentityAccounts.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail);
        var phoneIdentity = await _context.IdentityAccounts.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.NormalizedPhoneNumber == normalizedPhone);
        var candidateIdentityIds = new[] { linkedIdentity?.Id, emailIdentity?.Id, phoneIdentity?.Id }
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToArray();
        if (candidateIdentityIds.Length > 1)
            throw new InvalidOperationException("Platform bootstrap email and phone belong to different identities.");

        var passwordChanged = owner is null ||
            (_bootstrap.ResetPassword && !PasswordMatches(_bootstrap.Password!, owner.PasswordHash));
        var passwordHash = passwordChanged
            ? BCrypt.Net.BCrypt.HashPassword(_bootstrap.Password!)
            : owner!.PasswordHash;
        var identity = linkedIdentity ?? emailIdentity ?? phoneIdentity;
        if (identity is null)
        {
            identity = new IdentityAccount();
            _context.IdentityAccounts.Add(identity);
        }

        var emailChanged = !string.Equals(identity.NormalizedEmail, normalizedEmail, StringComparison.Ordinal);
        var phoneChanged = !string.Equals(identity.NormalizedPhoneNumber, normalizedPhone, StringComparison.Ordinal);
        identity.FullName = _bootstrap.FullName.Trim();
        identity.Email = _bootstrap.Email!.Trim();
        identity.NormalizedEmail = normalizedEmail;
        if (emailChanged || identity.EmailVerifiedAt is null)
            identity.EmailVerifiedAt = now;
        identity.PhoneNumber = normalizedPhone;
        identity.NormalizedPhoneNumber = normalizedPhone;
        if (phoneChanged || identity.PhoneVerifiedAt is null)
            identity.PhoneVerifiedAt = now;
        identity.PasswordHash = passwordHash;
        identity.IsActive = true;
        identity.FailedLoginAttempts = 0;
        identity.LockoutEndUtc = null;

        if (owner is null)
        {
            owner = new User
            {
                TenantId = PlatformConstants.PlatformTenantId,
                Role = UserRole.PlatformOwner
            };
            _context.Set<User>().Add(owner);
            _context.UserProfiles.Add(new UserProfile
            {
                UserId = owner.Id,
                FullName = _bootstrap.FullName.Trim()
            });
        }
        else
        {
            var profile = await _context.UserProfiles.IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.UserId == owner.Id);
            if (profile is null)
                _context.UserProfiles.Add(new UserProfile { UserId = owner.Id, FullName = _bootstrap.FullName.Trim() });
            else
            {
                profile.FullName = _bootstrap.FullName.Trim();
                profile.IsDeleted = false;
                profile.DeletedAt = null;
                profile.DeletedBy = null;
            }
        }

        owner.IdentityAccountId = identity.Id;
        owner.Email = _bootstrap.Email.Trim();
        owner.PhoneNumber = normalizedPhone;
        owner.PasswordHash = passwordHash;
        owner.IsActive = true;
        owner.IsDeleted = false;
        owner.DeletedAt = null;
        owner.DeletedBy = null;
        owner.FailedLoginAttempts = 0;
        owner.LockoutEndUtc = null;

        if (passwordChanged)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(x => x.UserId == owner.Id && x.RevokedAt == null)
                .ToListAsync();
            foreach (var token in activeTokens)
                token.RevokedAt = now;
        }

        await _context.SaveChangesAsync();
        _logger.LogWarning(
            "Platform Owner bootstrap completed without logging credentials. Disable and remove PlatformBootstrap secrets now.");
    }

    private static bool PasswordMatches(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
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
                DisplayNameAr = GetPermissionLabel(code),
                Category = Permissions.PlatformPermissions.Contains(code) ? "Platform" : "Tenant",
                IsPlatformPermission = Permissions.PlatformPermissions.Contains(code)
            });
        }

        var unlabeled = await _context.Permissions.Where(p => p.DisplayNameAr == "").ToListAsync();
        foreach (var permission in unlabeled)
            permission.DisplayNameAr = GetPermissionLabel(permission.Code);

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
                    NameAr = GetRoleLabel(roleName),
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

            // Older deployments seeded FreelanceOwner with all tenant permissions.
            // Remove only those stale grants for this system role and invalidate
            // its existing sessions so the reduced set is effective immediately.
            if (roleName == SystemRoles.FreelanceOwner)
            {
                var desiredIds = permissionCodes
                    .Where(permissionsByCode.ContainsKey)
                    .Select(code => permissionsByCode[code])
                    .ToHashSet();
                var staleMappings = await _context.RolePermissions
                    .Where(rp => rp.RoleId == role.Id && !desiredIds.Contains(rp.PermissionId))
                    .ToListAsync();
                if (staleMappings.Count > 0)
                {
                    _context.RolePermissions.RemoveRange(staleMappings);
                    var roleUserIds = await _context.UserRoleAssignments
                        .IgnoreQueryFilters()
                        .Where(assignment => assignment.RoleId == role.Id)
                        .Select(assignment => assignment.UserId)
                        .Distinct()
                        .ToListAsync();
                    var roleUsers = await _context.Set<User>()
                        .IgnoreQueryFilters()
                        .Where(user => roleUserIds.Contains(user.Id))
                        .ToListAsync();
                    foreach (var roleUser in roleUsers)
                        roleUser.PermissionsVersion++;
                    _logger.LogInformation(
                        "Removed {Count} stale Gym permissions from FreelanceOwner and invalidated {UserCount} sessions.",
                        staleMappings.Count, roleUsers.Count);
                }
            }
        }

        var unlabeledRoles = await _context.AppRoles.IgnoreQueryFilters().Where(r => r.NameAr == "").ToListAsync();
        foreach (var role in unlabeledRoles)
            role.NameAr = GetRoleLabel(role.Name);

        await _context.SaveChangesAsync();
    }

    private static readonly Dictionary<string, string> PermissionLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        [Permissions.ViewMembers] = "عرض العملاء", [Permissions.ManageMembers] = "إدارة العملاء",
        [Permissions.CreateMembers] = "إضافة العملاء", [Permissions.UpdateMembers] = "تعديل العملاء", [Permissions.DeleteMembers] = "حذف العملاء",
        [Permissions.ManageCoaches] = "إدارة المدربين", [Permissions.ManageAttendance] = "إدارة الحضور",
        [Permissions.ManageClientSubscriptions] = "إدارة اشتراكات العملاء", [Permissions.ManagePOS] = "إدارة نقاط البيع",
        [Permissions.ManageInventory] = "إدارة المخزون", [Permissions.ManageEmployees] = "إدارة الموظفين", [Permissions.ManageBranches] = "إدارة الفروع",
        [Permissions.ManageFinance] = "إدارة المالية", [Permissions.ViewReports] = "عرض التقارير", [Permissions.ManageReports] = "إدارة التقارير",
        [Permissions.ManageSettings] = "إدارة الإعدادات", [Permissions.ManageTenantBilling] = "إدارة فوترة الجيم", [Permissions.CreateAndDownloadTenantBackup] = "إنشاء وتنزيل نسخة Workspace",
        [Permissions.ManagePlatform] = "إدارة المنصة", [Permissions.ManageTenants] = "إدارة الجيمات", [Permissions.ManagePlans] = "إدارة الباقات",
        [Permissions.ManagePaymentRequests] = "إدارة طلبات الدفع", [Permissions.ManagePlatformReports] = "إدارة تقارير المنصة", [Permissions.ManagePlatformBackups] = "إدارة النسخ الاحتياطية"
    };

    private static readonly Dictionary<string, string> RoleLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        [SystemRoles.Owner] = "مالك الجيم", [SystemRoles.Manager] = "مدير الجيم", [SystemRoles.Receptionist] = "موظف الاستقبال",
        [SystemRoles.Accountant] = "محاسب", [SystemRoles.Coach] = "مدرب", [SystemRoles.Trainer] = "مدرب شخصي", [SystemRoles.Client] = "عميل",
        [SystemRoles.PlatformOwner] = "مالك المنصة", [SystemRoles.PlatformAdmin] = "مشرف المنصة"
    };

    private static string GetPermissionLabel(string code) => code switch
    {
        Permissions.ViewMembers => "\u0639\u0631\u0636 \u0627\u0644\u0639\u0645\u0644\u0627\u0621", Permissions.ManageMembers => "\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u0639\u0645\u0644\u0627\u0621",
        Permissions.CreateMembers => "\u0625\u0636\u0627\u0641\u0629 \u0627\u0644\u0639\u0645\u0644\u0627\u0621", Permissions.UpdateMembers => "\u062a\u0639\u062f\u064a\u0644 \u0627\u0644\u0639\u0645\u0644\u0627\u0621", Permissions.DeleteMembers => "\u062d\u0630\u0641 \u0627\u0644\u0639\u0645\u0644\u0627\u0621",
        Permissions.ManageCoaches => "\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u0645\u062f\u0631\u0628\u064a\u0646", Permissions.ManageAttendance => "\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u062d\u0636\u0648\u0631",
        Permissions.ManageClientSubscriptions => "\u0625\u062f\u0627\u0631\u0629 \u0627\u0634\u062a\u0631\u0627\u0643\u0627\u062a \u0627\u0644\u0639\u0645\u0644\u0627\u0621", Permissions.ManagePOS => "\u0625\u062f\u0627\u0631\u0629 \u0646\u0642\u0627\u0637 \u0627\u0644\u0628\u064a\u0639",
        Permissions.ManageInventory => "\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u0645\u062e\u0632\u0648\u0646", Permissions.ManageEmployees => "\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u0645\u0648\u0638\u0641\u064a\u0646", Permissions.ManageBranches => "\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u0641\u0631\u0648\u0639",
        Permissions.ManageFinance => "\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u0645\u0627\u0644\u064a\u0629", Permissions.ViewReports => "\u0639\u0631\u0636 \u0627\u0644\u062a\u0642\u0627\u0631\u064a\u0631", Permissions.ManageReports => "\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u062a\u0642\u0627\u0631\u064a\u0631",
        Permissions.ManageSettings => "\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u0625\u0639\u062f\u0627\u062f\u0627\u062a", Permissions.ManageTenantBilling => "\u0625\u062f\u0627\u0631\u0629 \u0641\u0648\u062a\u0631\u0629 \u0627\u0644\u062c\u064a\u0645", Permissions.CreateAndDownloadTenantBackup => "\u0625\u0646\u0634\u0627\u0621 \u0648\u062a\u0646\u0632\u064a\u0644 \u0646\u0633\u062e\u0629 Workspace",
        Permissions.ManagePlatform => "\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u0645\u0646\u0635\u0629", Permissions.ManageTenants => "\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u062c\u064a\u0645\u0627\u062a", Permissions.ManagePlans => "\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u0628\u0627\u0642\u0627\u062a",
        Permissions.ManagePaymentRequests => "\u0625\u062f\u0627\u0631\u0629 \u0637\u0644\u0628\u0627\u062a \u0627\u0644\u062f\u0641\u0639", Permissions.ManagePlatformReports => "\u0625\u062f\u0627\u0631\u0629 \u062a\u0642\u0627\u0631\u064a\u0631 \u0627\u0644\u0645\u0646\u0635\u0629", Permissions.ManagePlatformBackups => "\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u0646\u0633\u062e \u0627\u0644\u0627\u062d\u062a\u064a\u0627\u0637\u064a\u0629",
        _ => code
    };

    private static string GetRoleLabel(string name)
    {
        if (name == SystemRoles.FreelanceOwner) return "\u0645\u0627\u0644\u0643 \u0627\u0644\u0645\u062f\u0631\u0628 \u0627\u0644\u062d\u0631";
        if (name == SystemRoles.FreelanceCoach) return "\u0645\u062f\u0631\u0628 \u062d\u0631";
        if (name == SystemRoles.FreelanceAssistant) return "\u0645\u0633\u0627\u0639\u062f \u0645\u062f\u0631\u0628";

        return name switch
        {
            SystemRoles.Owner => "\u0645\u0627\u0644\u0643 \u0627\u0644\u062c\u064a\u0645", SystemRoles.Manager => "\u0645\u062f\u064a\u0631 \u0627\u0644\u062c\u064a\u0645", SystemRoles.Receptionist => "\u0645\u0648\u0638\u0641 \u0627\u0644\u0627\u0633\u062a\u0642\u0628\u0627\u0644", SystemRoles.Accountant => "\u0645\u062d\u0627\u0633\u0628", SystemRoles.Coach => "\u0645\u062f\u0631\u0628", SystemRoles.Trainer => "\u0645\u062f\u0631\u0628 \u0634\u062e\u0635\u064a", SystemRoles.Client => "\u0639\u0645\u064a\u0644", SystemRoles.PlatformOwner => "\u0645\u0627\u0644\u0643 \u0627\u0644\u0645\u0646\u0635\u0629", SystemRoles.PlatformAdmin => "\u0645\u0634\u0631\u0641 \u0627\u0644\u0645\u0646\u0635\u0629", _ => name
        };
    }

    private async Task BackfillUserRolesAsync()
    {
        // Preserve the legacy backfill for users with no assignments. In addition, reconcile
        // PlatformOwner/PlatformAdmin even when another assignment exists: older logic skipped
        // those accounts and could issue a Platform JWT without its required signed role.
        // Tenant users with an existing assignment are deliberately left unchanged because their
        // explicit RBAC assignment remains the source of truth during the legacy-role transition.
        var mappedRoles = LegacyRoleMap.Keys.ToArray();
        var users = await _context.Set<User>()
            .IgnoreQueryFilters()
            .Where(u => mappedRoles.Contains(u.Role))
            .ToListAsync();

        if (users.Count == 0) return;

        var systemRoles = await _context.AppRoles
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == null)
            .ToDictionaryAsync(r => r.Name, r => r.Id);

        var userIds = users.Select(x => x.Id).ToArray();
        var existingAssignments = (await _context.UserRoleAssignments
                .IgnoreQueryFilters()
                .Where(ur => userIds.Contains(ur.UserId))
                .Select(ur => new { ur.UserId, ur.RoleId })
                .ToListAsync())
            .Select(x => (x.UserId, x.RoleId))
            .ToHashSet();
        var assignedUserIds = existingAssignments.Select(x => x.UserId).ToHashSet();

        var added = 0;
        foreach (var user in users)
        {
            if (!LegacyRoleMap.TryGetValue(user.Role, out var roleName)) continue;
            if (!systemRoles.TryGetValue(roleName, out var roleId)) continue;
            if (existingAssignments.Contains((user.Id, roleId))) continue;
            var isPlatformRole = user.Role is UserRole.PlatformOwner or UserRole.PlatformAdmin;
            if (!isPlatformRole && assignedUserIds.Contains(user.Id)) continue;

            _context.UserRoleAssignments.Add(new UserRoleAssignment
            {
                UserId = user.Id,
                RoleId = roleId,
                TenantId = isPlatformRole ? null : user.TenantId
            });
            user.PermissionsVersion++;
            existingAssignments.Add((user.Id, roleId));
            assignedUserIds.Add(user.Id);
            added++;
        }

        if (added > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Backfilled {Count} user-role assignments", added);
        }
    }
}

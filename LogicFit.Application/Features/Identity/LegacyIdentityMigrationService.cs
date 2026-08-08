using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Identity;

/// <summary>
/// Migrates an already-authenticated legacy tenant user into the identity-first model. The
/// migration is deliberately password-bound and only runs when no identity exists for the email;
/// it never resets an existing identity or grants access to an inactive/deleted user/workspace.
/// </summary>
public sealed class LegacyIdentityMigrationService
{
    private readonly IApplicationDbContext _context;
    private readonly IRbacService _rbacService;
    private readonly IDateTimeService _clock;

    public LegacyIdentityMigrationService(
        IApplicationDbContext context,
        IRbacService rbacService,
        IDateTimeService clock)
        => (_context, _rbacService, _clock) = (context, rbacService, clock);

    public async Task<IdentityAccount?> TryMigrateAsync(
        string normalizedEmail,
        string password,
        CancellationToken cancellationToken = default)
    {
        var legacyUsers = await _context.Users
            .IgnoreQueryFilters()
            .Include(x => x.Profile)
            .Where(x => x.Email.ToUpper() == normalizedEmail && x.IsActive && !x.IsDeleted &&
                x.Role != UserRole.PlatformOwner && x.Role != UserRole.PlatformAdmin)
            .ToListAsync(cancellationToken);

        var passwordMatches = legacyUsers
            .Where(x => PasswordMatches(password, x.PasswordHash))
            .ToList();
        if (passwordMatches.Count == 0)
            return null;

        var tenantIds = passwordMatches.Select(x => x.TenantId).Distinct().ToArray();
        var tenants = await _context.Tenants
            .IgnoreQueryFilters()
            .Where(x => tenantIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var userIds = passwordMatches.Select(x => x.Id).ToArray();
        var existingMemberships = await _context.WorkspaceMemberships
            .IgnoreQueryFilters()
            .Where(x => userIds.Contains(x.UserId) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.UserId, cancellationToken);

        // Do not create an identity that would have no usable workspace membership.
        var migratableUsers = passwordMatches
            .Where(x => tenants.ContainsKey(x.TenantId) &&
                (!existingMemberships.TryGetValue(x.Id, out var membership) ||
                 membership.IdentityAccountId == x.IdentityAccountId))
            .ToList();
        if (migratableUsers.Count == 0)
            return null;

        var firstUser = migratableUsers[0];
        var now = _clock.UtcNow;
        var identity = new IdentityAccount
        {
            FullName = string.IsNullOrWhiteSpace(firstUser.Profile?.FullName)
                ? firstUser.Email
                : firstUser.Profile.FullName,
            Email = firstUser.Email,
            NormalizedEmail = normalizedEmail,
            PhoneNumber = firstUser.PhoneNumber,
            PasswordHash = firstUser.PasswordHash,
            // A successful legacy password login is the proof used for this one-time migration.
            // Future registrations still require the normal email-verification link.
            EmailVerifiedAt = now,
            IsActive = true
        };
        _context.IdentityAccounts.Add(identity);

        foreach (var user in migratableUsers)
        {
            var tenant = tenants[user.TenantId];
            user.IdentityAccountId = identity.Id;

            if (!existingMemberships.ContainsKey(user.Id))
            {
                var status = tenant.Status == TenantStatus.Active
                    ? WorkspaceMembershipStatus.Active
                    : WorkspaceMembershipStatus.PendingPlatformApproval;
                _context.WorkspaceMemberships.Add(new WorkspaceMembership
                {
                    TenantId = user.TenantId,
                    IdentityAccountId = identity.Id,
                    UserId = user.Id,
                    Role = user.Role,
                    Status = status,
                    ApprovedAt = status == WorkspaceMembershipStatus.Active ? now : null,
                    ApprovedBy = status == WorkspaceMembershipStatus.Active ? "identity-legacy-migration" : null
                });
            }

            var systemRole = SystemRoleFor(user.Role);
            if (systemRole is not null)
                await _rbacService.EnsureUserInRoleAsync(user.Id, user.TenantId, systemRole, cancellationToken);
        }

        return identity;
    }

    private static bool PasswordMatches(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            // A malformed legacy hash is treated as a failed credential, never as a server error.
            return false;
        }
    }

    private static string? SystemRoleFor(UserRole role) => role switch
    {
        UserRole.Owner => SystemRoles.Owner,
        UserRole.Manager => SystemRoles.Manager,
        UserRole.Receptionist => SystemRoles.Receptionist,
        UserRole.Accountant => SystemRoles.Accountant,
        UserRole.Coach => SystemRoles.Coach,
        UserRole.Client => SystemRoles.Client,
        UserRole.Trainer => SystemRoles.Trainer,
        UserRole.FreelanceOwner => SystemRoles.FreelanceOwner,
        UserRole.FreelanceCoach => SystemRoles.FreelanceCoach,
        UserRole.FreelanceAssistant => SystemRoles.FreelanceAssistant,
        _ => null
    };
}

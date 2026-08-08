using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkspaceMembers.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.WorkspaceMembers;

internal static class WorkspaceMemberMapping
{
    private static readonly IReadOnlyDictionary<UserRole, string> RoleNames = new Dictionary<UserRole, string>
    {
        [UserRole.Coach] = Domain.Authorization.SystemRoles.Coach,
        [UserRole.Manager] = Domain.Authorization.SystemRoles.Manager,
        [UserRole.Receptionist] = Domain.Authorization.SystemRoles.Receptionist,
        [UserRole.Accountant] = Domain.Authorization.SystemRoles.Accountant,
        [UserRole.Trainer] = Domain.Authorization.SystemRoles.Trainer
    };

    public static bool IsAllowedRole(UserRole role) => RoleNames.ContainsKey(role);

    public static string RoleName(UserRole role) => RoleNames.TryGetValue(role, out var name) ? name : role.ToString();

    public static WorkspaceMemberDto ToDto(WorkspaceMembership membership, DateTime now)
    {
        var user = membership.User;
        var identity = membership.IdentityAccount;
        return new WorkspaceMemberDto
        {
            MembershipId = membership.Id,
            UserId = user.Id,
            IdentityAccountId = identity.Id,
            TenantId = membership.TenantId,
            Email = identity.Email,
            PhoneNumber = user.PhoneNumber ?? identity.PhoneNumber,
            FullName = user.Profile?.FullName ?? identity.FullName,
            Role = membership.Role,
            RoleName = RoleName(membership.Role),
            MembershipStatus = membership.Status,
            AccessStatus = ResolveAccessStatus(membership, now),
            MustChangePassword = user.MustChangePassword,
            IsActive = user.IsActive && !user.IsDeleted && membership.Status == WorkspaceMembershipStatus.Active && !membership.IsDeleted,
            UpdatedAtUtc = membership.UpdatedAt ?? user.UpdatedAt ?? identity.UpdatedAt
        };
    }

    public static string ResolveAccessStatus(WorkspaceMembership membership, DateTime now)
    {
        if (membership.IsDeleted || membership.Status == WorkspaceMembershipStatus.Revoked || membership.User.IsDeleted)
            return WorkspaceMemberAccessStatuses.Removed;
        if (membership.Status == WorkspaceMembershipStatus.Suspended || !membership.User.IsActive)
            return WorkspaceMemberAccessStatuses.Suspended;
        if (membership.IdentityAccount.LockoutEndUtc > now)
            return WorkspaceMemberAccessStatuses.Locked;
        if (membership.Status == WorkspaceMembershipStatus.Invited)
            return WorkspaceMemberAccessStatuses.PendingSetup;
        if (membership.User.MustChangePassword)
            return WorkspaceMemberAccessStatuses.PasswordChangeRequired;
        return WorkspaceMemberAccessStatuses.Active;
    }
}

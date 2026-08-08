using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.WorkspaceMembers.DTOs;

public static class WorkspaceMemberAccessStatuses
{
    public const string PendingSetup = "PendingSetup";
    public const string PasswordChangeRequired = "PasswordChangeRequired";
    public const string Active = "Active";
    public const string Suspended = "Suspended";
    public const string Locked = "Locked";
    public const string Removed = "Removed";
}

public sealed class WorkspaceMemberDto
{
    public Guid MembershipId { get; init; }
    public Guid UserId { get; init; }
    public Guid IdentityAccountId { get; init; }
    public Guid TenantId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? FullName { get; init; }
    public UserRole Role { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public WorkspaceMembershipStatus MembershipStatus { get; init; }
    public string AccessStatus { get; init; } = WorkspaceMemberAccessStatuses.Active;
    public bool MustChangePassword { get; init; }
    public bool IsActive { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class OneTimeWorkspaceMemberCredentialsDto
{
    public string Email { get; init; } = string.Empty;
    public string TemporaryPassword { get; init; } = string.Empty;
    public bool MustChangePassword { get; init; } = true;
}

public sealed class WorkspaceMemberCreatedDto
{
    public WorkspaceMemberDto Member { get; init; } = new();
    public bool NewIdentity { get; init; }
    public OneTimeWorkspaceMemberCredentialsDto? OneTimeCredentials { get; init; }
}

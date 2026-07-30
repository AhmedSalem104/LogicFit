using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

/// <summary>One-use email-bound invitation. The raw link token is never persisted.</summary>
public sealed class WorkspaceInvite : AuditableEntity
{
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid InvitedByMembershipId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public WorkspaceInviteStatus Status { get; set; } = WorkspaceInviteStatus.Pending;
    public DateTime? AcceptedAt { get; set; }
    public Guid? AcceptedIdentityAccountId { get; set; }
    public DateTime? RevokedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Tenant Tenant { get; set; } = null!;
    public WorkspaceMembership InvitedByMembership { get; set; } = null!;
}

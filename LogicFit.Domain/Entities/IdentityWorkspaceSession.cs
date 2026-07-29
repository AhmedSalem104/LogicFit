using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>
/// Short-lived opaque proof created after an identity-first sign-in. It can select a workspace but
/// is not a tenant JWT and has no refresh capability.
/// </summary>
public class IdentityWorkspaceSession : BaseEntity
{
    public Guid IdentityAccountId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? CreatedByIp { get; set; }
    public IdentityAccount IdentityAccount { get; set; } = null!;
}

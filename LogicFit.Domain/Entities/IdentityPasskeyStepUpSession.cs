using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>Short-lived proof of a recently verified passkey for a sensitive server mutation.</summary>
public sealed class IdentityPasskeyStepUpSession : AuditableEntity
{
    public Guid IdentityAccountId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public IdentityAccount IdentityAccount { get; set; } = null!;
}

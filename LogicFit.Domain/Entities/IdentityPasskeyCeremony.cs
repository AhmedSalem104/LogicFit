using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

/// <summary>Short-lived server-side WebAuthn challenge. Original browser options are retained only until one use or expiry.</summary>
public sealed class IdentityPasskeyCeremony : AuditableEntity
{
    public Guid IdentityAccountId { get; set; }
    public IdentityPasskeyCeremonyPurpose Purpose { get; set; }
    public string OptionsJson { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public IdentityAccount IdentityAccount { get; set; } = null!;
}

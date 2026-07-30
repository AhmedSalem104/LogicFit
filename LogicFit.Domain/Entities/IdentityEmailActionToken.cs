using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

/// <summary>
/// Persisted hash of a short-lived, one-time email action link. The raw token never reaches the
/// database, audit log, or application log.
/// </summary>
public class IdentityEmailActionToken : AuditableEntity
{
    public Guid IdentityAccountId { get; set; }
    public EmailActionTokenPurpose Purpose { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? CreatedByIp { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public IdentityAccount IdentityAccount { get; set; } = null!;
}

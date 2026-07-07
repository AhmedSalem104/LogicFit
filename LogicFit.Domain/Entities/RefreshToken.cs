using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>
/// A persisted refresh token supporting rotation and revocation. Looked up by its opaque
/// <see cref="Token"/> value; not tenant query-filtered (the token itself is the secret).
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    // Which API surface issued this token ("tenant" or "platform"). A token may only be rotated
    // on the surface that issued it, preventing a platform token from minting a tenant token.
    public string Surface { get; set; } = "tenant";
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }

    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;

    // Navigation
    public virtual User User { get; set; } = null!;
}

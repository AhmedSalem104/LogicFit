using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>
/// A short-lived, single-use proof that a user re-entered their password for one sensitive
/// operation. Only the SHA-256 hash of the opaque token is persisted.
/// </summary>
public sealed class SensitiveActionGrant : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? CreatedByIp { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

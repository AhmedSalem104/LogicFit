using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>Single-use, short-lived download authorization for one tenant export.</summary>
public sealed class TenantBackupDownloadGrant : AuditableEntity
{
    public Guid TenantBackupExportId { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? CreatedByIp { get; set; }
    public string? ConsumedByIp { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public TenantBackupExport TenantBackupExport { get; set; } = null!;
}

using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

/// <summary>One BACPAC export requested by a user for the current workspace.</summary>
public sealed class TenantBackupExport : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? BackupBatchId { get; set; }
    public Guid? DatabaseBackupId { get; set; }
    public TenantBackupExportStatus Status { get; set; } = TenantBackupExportStatus.Pending;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? DownloadedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public BackupBatch? BackupBatch { get; set; }
    public DatabaseBackup? DatabaseBackup { get; set; }
    public ICollection<TenantBackupDownloadGrant> DownloadGrants { get; set; } = new List<TenantBackupDownloadGrant>();
}

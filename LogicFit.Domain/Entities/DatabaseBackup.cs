using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

/// <summary>Metadata for one private, independently downloadable database export.</summary>
public sealed class DatabaseBackup : AuditableEntity
{
    public Guid BackupBatchId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? DatabaseResourceId { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string? StorageKey { get; set; }
    public long SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DatabaseBackupStatus Status { get; set; } = DatabaseBackupStatus.Pending;
    public string? ErrorMessage { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public BackupBatch BackupBatch { get; set; } = null!;
}

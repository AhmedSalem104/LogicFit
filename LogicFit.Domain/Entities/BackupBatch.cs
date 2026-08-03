using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

/// <summary>Central record for one independent platform/tenant backup operation.</summary>
public sealed class BackupBatch : AuditableEntity
{
    public BackupScope Scope { get; set; }
    public BackupBatchStatus Status { get; set; } = BackupBatchStatus.Pending;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? ManifestStorageKey { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<DatabaseBackup> Artifacts { get; set; } = new List<DatabaseBackup>();
}

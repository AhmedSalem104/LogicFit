using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

/// <summary>Platform audit/job record for a conditional, provider-backed tenant restore.</summary>
public sealed class RestoreJob : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid SourceDatabaseBackupId { get; set; }
    public Guid? TargetDatabaseResourceId { get; set; }
    public RestoreJobStatus Status { get; set; } = RestoreJobStatus.Pending;
    public string Provider { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string WorkspaceNameConfirmation { get; set; } = string.Empty;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorCode { get; set; }
    public Guid? PreviousMappingId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

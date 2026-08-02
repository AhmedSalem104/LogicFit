using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

/// <summary>Persistent, retryable cross-database provisioning intent in Platform DB.</summary>
public sealed class ProvisioningJob : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ApplicationRequestId { get; set; }
    public Guid? DatabaseResourceId { get; set; }
    public ProvisioningJobStatus Status { get; set; } = ProvisioningJobStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public string? LastErrorCode { get; set; }
    public string? LastError { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

/// <summary>
/// A database registered by an operator in the Monster resource pool.  The real database name
/// is never inferred from a TenantId and the encrypted connection data is never exposed by APIs.
/// </summary>
public sealed class DatabaseResource : AuditableEntity
{
    public string Provider { get; set; } = "ManualMonster";
    public string DatabaseName { get; set; } = string.Empty;
    public string? ServerKey { get; set; }
    public string? EncryptedConnectionString { get; set; }
    public DatabaseResourceStatus Status { get; set; } = DatabaseResourceStatus.Available;
    public Guid? ReservedForTenantId { get; set; }
    public DateTime? ReservedAtUtc { get; set; }
    public DateTime? AssignedAtUtc { get; set; }
    public DateTime? LastHealthCheckAtUtc { get; set; }
    public long? SizeBytes { get; set; }
    public string? SchemaVersion { get; set; }
    public string? LastError { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

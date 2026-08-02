using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>
/// Server-side mapping from a Workspace to its assigned database resource.  Connection material is
/// protected at rest and intentionally has no API DTO/navigation that can cross a tenant boundary.
/// </summary>
public sealed class TenantDatabaseMapping : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid DatabaseResourceId { get; set; }
    public string Provider { get; set; } = "ManualMonster";
    public string EncryptedConnectionString { get; set; } = string.Empty;
    public string? SchemaVersion { get; set; }
    public DateTime? LastValidatedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>Reusable public client-join secret for one workspace. The displayed QR/code is never stored in raw form.</summary>
public sealed class WorkspaceClientJoinCode : AuditableEntity
{
    public Guid TenantId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool AutoApproveClients { get; set; }
    public DateTime? RevokedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Tenant Tenant { get; set; } = null!;
}

using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? TenantId { get; set; }
    public string? UserId { get; set; }
    public AuditAction Action { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? AffectedColumns { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
}

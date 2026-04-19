using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class MaintenanceRecord : TenantAuditableEntity
{
    public Guid EquipmentId { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public decimal? Cost { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? TechnicianName { get; set; }
    public string? TechnicianContact { get; set; }
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Pending;
    public string? ResolutionNotes { get; set; }

    public virtual Equipment Equipment { get; set; } = null!;
}

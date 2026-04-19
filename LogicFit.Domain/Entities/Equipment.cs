using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class Equipment : TenantAuditableEntity
{
    public Guid BranchId { get; set; }
    public Guid? RoomId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Category { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Active;
    public DateTime? WarrantyUntil { get; set; }
    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }

    public virtual Branch Branch { get; set; } = null!;
    public virtual Room? Room { get; set; }
    public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
}

using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Equipment.DTOs;

public class EquipmentDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public string? BranchName { get; set; }
    public Guid? RoomId { get; set; }
    public string? RoomName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Category { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public EquipmentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime? WarrantyUntil { get; set; }
    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }
    public int OpenMaintenanceCount { get; set; }
}

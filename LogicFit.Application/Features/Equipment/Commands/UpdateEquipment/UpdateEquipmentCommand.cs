using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Equipment.Commands.UpdateEquipment;

public class UpdateEquipmentCommand : IRequest
{
    public Guid Id { get; set; }
    public Guid? RoomId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Category { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public EquipmentStatus Status { get; set; }
    public DateTime? WarrantyUntil { get; set; }
    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }
}

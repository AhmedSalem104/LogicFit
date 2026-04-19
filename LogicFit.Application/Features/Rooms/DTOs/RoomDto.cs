using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Rooms.DTOs;

public class RoomDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public string? BranchName { get; set; }
    public string Name { get; set; } = string.Empty;
    public RoomType Type { get; set; }
    public string TypeName => Type.ToString();
    public int? Capacity { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int EquipmentCount { get; set; }
}

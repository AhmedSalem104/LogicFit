using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class Room : TenantAuditableEntity
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public RoomType Type { get; set; }
    public int? Capacity { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
}

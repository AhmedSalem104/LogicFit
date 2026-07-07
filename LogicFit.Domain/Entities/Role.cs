using LogicFit.Domain.Common;
using LogicFit.Domain.Common.Interfaces;

namespace LogicFit.Domain.Entities;

/// <summary>
/// A role groups permissions. TenantId == null marks a shared system role template
/// (Owner, Manager, ..., PlatformOwner) that cannot be edited; TenantId set marks a
/// custom role a gym owner created for their own tenant.
/// </summary>
public class Role : AuditableEntity, ISoftDeletable
{
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public virtual ICollection<UserRoleAssignment> UserRoles { get; set; } = new List<UserRoleAssignment>();
}

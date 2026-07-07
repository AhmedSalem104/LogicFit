
using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>
/// Global reference data: one row per permission code (see LogicFit.Domain.Authorization.Permissions).
/// Not tenant-scoped, not soft-deleted.
/// </summary>
public class Permission : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsPlatformPermission { get; set; }

    // Navigation
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>Join between <see cref="Role"/> and <see cref="Permission"/>.</summary>
public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    // Navigation
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}

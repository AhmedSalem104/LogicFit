using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>
/// Join between a <see cref="User"/> and a <see cref="Role"/>. The new source of truth for
/// authorization (the legacy <see cref="Enums.UserRole"/> enum on User is kept in sync during
/// the transition). TenantId is null for platform users.
/// </summary>
public class UserRoleAssignment : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid? TenantId { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}

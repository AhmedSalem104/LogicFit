using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class UserBranchAccess : TenantAuditableEntity
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}

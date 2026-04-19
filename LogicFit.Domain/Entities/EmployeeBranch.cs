using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class EmployeeBranch : TenantAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid BranchId { get; set; }
    public bool IsPrimary { get; set; }

    public virtual EmployeeProfile Employee { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}

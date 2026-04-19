using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class CommissionRule : TenantAuditableEntity
{
    public Guid? EmployeeId { get; set; } // specific employee
    public UserRole? Role { get; set; } // OR role-based
    public CommissionSourceType SourceType { get; set; }
    public CommissionRuleType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MinAmount { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual EmployeeProfile? Employee { get; set; }
}

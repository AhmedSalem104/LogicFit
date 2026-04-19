using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class Commission : TenantAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public CommissionSourceType SourceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public decimal Amount { get; set; }
    public decimal SourceAmount { get; set; }
    public DateTime EarnedDate { get; set; }
    public CommissionStatus Status { get; set; } = CommissionStatus.Pending;
    public Guid? PayrollItemId { get; set; }
    public string? Description { get; set; }

    public virtual EmployeeProfile Employee { get; set; } = null!;
    public virtual PayrollItem? PayrollItem { get; set; }
}

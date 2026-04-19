using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class PayrollItem : TenantAuditableEntity
{
    public Guid PayrollRunId { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal CommissionTotal { get; set; }
    public decimal Bonus { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetSalary { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }

    public virtual PayrollRun PayrollRun { get; set; } = null!;
    public virtual EmployeeProfile Employee { get; set; } = null!;
    public virtual ICollection<Commission> Commissions { get; set; } = new List<Commission>();
}

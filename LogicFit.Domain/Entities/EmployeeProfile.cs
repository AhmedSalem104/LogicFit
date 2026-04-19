using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class EmployeeProfile : TenantAuditableEntity
{
    public Guid UserId { get; set; }
    public string? EmployeeCode { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public DateTime JoinDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public decimal BaseSalary { get; set; }
    public SalaryType SalaryType { get; set; } = SalaryType.Monthly;
    public decimal? HourlyRate { get; set; }
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public string? NationalId { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Qualifications { get; set; }
    public string? Notes { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual ICollection<EmployeeBranch> Branches { get; set; } = new List<EmployeeBranch>();
}

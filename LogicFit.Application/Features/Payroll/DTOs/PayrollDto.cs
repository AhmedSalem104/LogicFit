using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Payroll.DTOs;

public class PayrollRunDto
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public PayrollStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public decimal TotalAmount { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
    public int ItemsCount { get; set; }
    public List<PayrollItemDto> Items { get; set; } = new();
}

public class PayrollItemDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal CommissionTotal { get; set; }
    public decimal Bonus { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetSalary { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
}

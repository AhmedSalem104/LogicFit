using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Commissions.DTOs;

public class CommissionDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public CommissionSourceType SourceType { get; set; }
    public string SourceTypeName => SourceType.ToString();
    public Guid? ReferenceId { get; set; }
    public decimal Amount { get; set; }
    public decimal SourceAmount { get; set; }
    public DateTime EarnedDate { get; set; }
    public CommissionStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public Guid? PayrollItemId { get; set; }
    public string? Description { get; set; }
}

public class CommissionRuleDto
{
    public Guid Id { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public UserRole? Role { get; set; }
    public CommissionSourceType SourceType { get; set; }
    public CommissionRuleType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MinAmount { get; set; }
    public bool IsActive { get; set; }
}

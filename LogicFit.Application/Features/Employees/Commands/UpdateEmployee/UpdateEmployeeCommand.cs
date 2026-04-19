using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Employees.Commands.UpdateEmployee;

public class UpdateEmployeeCommand : IRequest
{
    public Guid Id { get; set; }
    public string? EmployeeCode { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public DateTime JoinDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public decimal BaseSalary { get; set; }
    public SalaryType SalaryType { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public string? NationalId { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Qualifications { get; set; }
    public List<Guid>? BranchIds { get; set; }
}

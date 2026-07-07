using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Employees.Commands.CreateEmployee;

public class CreateEmployeeCommand : IRequest<Guid>, IRequireQuota, IRequireFeature
{
    public string QuotaResource => QuotaResources.Employees;
    public string RequiredFeatureCode => FeatureCodes.EmployeeManagement;

    public Guid UserId { get; set; }
    public string? EmployeeCode { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public DateTime? JoinDate { get; set; }
    public decimal BaseSalary { get; set; }
    public SalaryType SalaryType { get; set; } = SalaryType.Monthly;
    public decimal? HourlyRate { get; set; }
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public string? NationalId { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Qualifications { get; set; }
    public List<Guid>? BranchIds { get; set; }
}

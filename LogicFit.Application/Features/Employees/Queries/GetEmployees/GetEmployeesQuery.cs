using LogicFit.Application.Features.Employees.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Employees.Queries.GetEmployees;

public class GetEmployeesQuery : IRequest<List<EmployeeDto>>
{
    public Guid? BranchId { get; set; }
    public string? Department { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
}

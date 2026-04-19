using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Employees.Commands.UpdateEmployee;

public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateEmployeeCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var employee = await _context.EmployeeProfiles
            .Include(e => e.Branches)
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Employee", request.Id);

        employee.EmployeeCode = request.EmployeeCode;
        employee.JobTitle = request.JobTitle;
        employee.Department = request.Department;
        employee.JoinDate = request.JoinDate;
        employee.TerminationDate = request.TerminationDate;
        employee.BaseSalary = request.BaseSalary;
        employee.SalaryType = request.SalaryType;
        employee.HourlyRate = request.HourlyRate;
        employee.BankAccount = request.BankAccount;
        employee.BankName = request.BankName;
        employee.NationalId = request.NationalId;
        employee.EmergencyContactName = request.EmergencyContactName;
        employee.EmergencyContactPhone = request.EmergencyContactPhone;
        employee.Qualifications = request.Qualifications;

        if (request.BranchIds != null)
        {
            foreach (var existing in employee.Branches.ToList())
                _context.EmployeeBranches.Remove(existing);

            var first = true;
            foreach (var bid in request.BranchIds.Distinct())
            {
                _context.EmployeeBranches.Add(new EmployeeBranch
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    EmployeeId = employee.Id,
                    BranchId = bid,
                    IsPrimary = first
                });
                first = false;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Employees.Commands.CreateEmployee;

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public CreateEmployeeCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Guid> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && u.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        var existing = await _context.EmployeeProfiles.AnyAsync(e => e.UserId == request.UserId, cancellationToken);
        if (existing)
            throw new ConflictException("Employee profile already exists for this user");

        var employee = new EmployeeProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = request.UserId,
            EmployeeCode = request.EmployeeCode,
            JobTitle = request.JobTitle,
            Department = request.Department,
            JoinDate = request.JoinDate ?? _dateTimeService.UtcNow,
            BaseSalary = request.BaseSalary,
            SalaryType = request.SalaryType,
            HourlyRate = request.HourlyRate,
            BankAccount = request.BankAccount,
            BankName = request.BankName,
            NationalId = request.NationalId,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            Qualifications = request.Qualifications
        };

        _context.EmployeeProfiles.Add(employee);

        if (request.BranchIds != null)
        {
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
        return employee.Id;
    }
}

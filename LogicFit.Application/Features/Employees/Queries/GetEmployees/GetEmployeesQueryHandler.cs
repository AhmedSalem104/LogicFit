using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Employees.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Employees.Queries.GetEmployees;

public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, List<EmployeeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetEmployeesQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.EmployeeProfiles
            .Include(e => e.User)
            .Include(e => e.Branches)
            .Where(e => e.TenantId == tenantId)
            .AsQueryable();

        if (request.BranchId.HasValue)
            query = query.Where(e => e.Branches.Any(b => b.BranchId == request.BranchId.Value));
        if (!string.IsNullOrWhiteSpace(request.Department))
            query = query.Where(e => e.Department == request.Department);
        if (request.IsActive == true)
            query = query.Where(e => e.TerminationDate == null);
        else if (request.IsActive == false)
            query = query.Where(e => e.TerminationDate != null);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(e => e.User.Email.Contains(term)
                || (e.JobTitle != null && e.JobTitle.Contains(term))
                || (e.EmployeeCode != null && e.EmployeeCode.Contains(term)));
        }

        var employees = await query.OrderBy(e => e.User.Email).ToListAsync(cancellationToken);

        return employees.Select(e => new EmployeeDto
        {
            Id = e.Id,
            UserId = e.UserId,
            Email = e.User.Email,
            PhoneNumber = e.User.PhoneNumber,
            Role = e.User.Role,
            EmployeeCode = e.EmployeeCode,
            JobTitle = e.JobTitle,
            Department = e.Department,
            JoinDate = e.JoinDate,
            TerminationDate = e.TerminationDate,
            BaseSalary = e.BaseSalary,
            SalaryType = e.SalaryType,
            HourlyRate = e.HourlyRate,
            BankAccount = e.BankAccount,
            BankName = e.BankName,
            NationalId = e.NationalId,
            EmergencyContactName = e.EmergencyContactName,
            EmergencyContactPhone = e.EmergencyContactPhone,
            Qualifications = e.Qualifications,
            BranchIds = e.Branches.Select(b => b.BranchId).ToList()
        }).ToList();
    }
}

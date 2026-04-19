using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Payroll.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Payroll.Queries.GetPayrollRuns;

public class GetPayrollRunsQueryHandler : IRequestHandler<GetPayrollRunsQuery, List<PayrollRunDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetPayrollRunsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<PayrollRunDto>> Handle(GetPayrollRunsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var query = _context.PayrollRuns
            .Include(r => r.Branch)
            .Include(r => r.Items).ThenInclude(i => i.Employee).ThenInclude(e => e.User)
            .Where(r => r.TenantId == tenantId).AsQueryable();

        if (request.Year.HasValue) query = query.Where(r => r.Year == request.Year.Value);
        if (request.Month.HasValue) query = query.Where(r => r.Month == request.Month.Value);
        if (request.BranchId.HasValue) query = query.Where(r => r.BranchId == request.BranchId.Value);
        if (request.Status.HasValue) query = query.Where(r => r.Status == request.Status.Value);

        var runs = await query.OrderByDescending(r => r.Year).ThenByDescending(r => r.Month).ToListAsync(cancellationToken);

        return runs.Select(r => new PayrollRunDto
        {
            Id = r.Id,
            BranchId = r.BranchId,
            BranchName = r.Branch?.Name,
            Month = r.Month,
            Year = r.Year,
            Status = r.Status,
            TotalAmount = r.TotalAmount,
            ApprovedAt = r.ApprovedAt,
            PaidAt = r.PaidAt,
            Notes = r.Notes,
            ItemsCount = r.Items.Count,
            Items = r.Items.Select(i => new PayrollItemDto
            {
                Id = i.Id,
                EmployeeId = i.EmployeeId,
                EmployeeName = i.Employee.User.Email,
                BaseSalary = i.BaseSalary,
                CommissionTotal = i.CommissionTotal,
                Bonus = i.Bonus,
                Deductions = i.Deductions,
                NetSalary = i.NetSalary,
                PaidAt = i.PaidAt,
                Notes = i.Notes
            }).ToList()
        }).ToList();
    }
}

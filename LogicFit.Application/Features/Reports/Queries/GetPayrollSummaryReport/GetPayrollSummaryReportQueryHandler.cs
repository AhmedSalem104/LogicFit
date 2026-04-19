using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetPayrollSummaryReport;

public class GetPayrollSummaryReportQueryHandler : IRequestHandler<GetPayrollSummaryReportQuery, PayrollSummaryReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public GetPayrollSummaryReportQueryHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<PayrollSummaryReportDto> Handle(GetPayrollSummaryReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;
        var year = request.Year ?? now.Year;

        var runsQuery = _context.PayrollRuns
            .Include(r => r.Items).ThenInclude(i => i.Employee)
            .Include(r => r.Branch)
            .Where(r => r.TenantId == tenantId && r.Year == year && r.Status != PayrollStatus.Cancelled);

        if (request.Month.HasValue)
            runsQuery = runsQuery.Where(r => r.Month == request.Month.Value);

        var runs = await runsQuery.ToListAsync(cancellationToken);
        var allItems = runs.SelectMany(r => r.Items).ToList();

        var pendingCommissions = await _context.Commissions
            .Where(c => c.TenantId == tenantId && c.Status == CommissionStatus.Pending)
            .ToListAsync(cancellationToken);

        var byBranch = runs
            .GroupBy(r => new { r.BranchId, BranchName = r.Branch?.Name ?? "(All)" })
            .Select(g => new PayrollByBranchDto
            {
                BranchId = g.Key.BranchId,
                BranchName = g.Key.BranchName,
                EmployeesCount = g.SelectMany(r => r.Items).Select(i => i.EmployeeId).Distinct().Count(),
                TotalNetSalaries = g.SelectMany(r => r.Items).Sum(i => i.NetSalary)
            })
            .OrderByDescending(b => b.TotalNetSalaries)
            .ToList();

        return new PayrollSummaryReportDto
        {
            Year = year,
            Month = request.Month,
            TotalBaseSalaries = allItems.Sum(i => i.BaseSalary),
            TotalCommissions = allItems.Sum(i => i.CommissionTotal),
            TotalBonuses = allItems.Sum(i => i.Bonus),
            TotalDeductions = allItems.Sum(i => i.Deductions),
            TotalNetSalaries = allItems.Sum(i => i.NetSalary),
            EmployeesPaid = allItems.Where(i => i.PaidAt.HasValue).Select(i => i.EmployeeId).Distinct().Count(),
            PendingCommissionsCount = pendingCommissions.Count,
            PendingCommissionsAmount = pendingCommissions.Sum(c => c.Amount),
            ByBranch = byBranch
        };
    }
}

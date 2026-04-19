using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetExpensesReport;

public class GetExpensesReportQueryHandler : IRequestHandler<GetExpensesReportQuery, ExpensesReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public GetExpensesReportQueryHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<ExpensesReportDto> Handle(GetExpensesReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;
        var from = request.FromDate ?? new DateTime(now.Year, now.Month, 1);
        var to = request.ToDate ?? now;

        var query = _context.Expenses
            .Include(e => e.Category)
            .Include(e => e.Branch)
            .Where(e => e.TenantId == tenantId && e.ExpenseDate >= from && e.ExpenseDate <= to);

        if (request.BranchId.HasValue)
            query = query.Where(e => e.BranchId == request.BranchId.Value);

        var expenses = await query.ToListAsync(cancellationToken);

        var total = expenses.Sum(e => e.Amount);

        var byCategory = expenses
            .GroupBy(e => new { e.CategoryId, e.Category.Name })
            .Select(g => new ExpenseByCategoryDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                TotalAmount = g.Sum(e => e.Amount),
                Count = g.Count(),
                Percentage = total > 0 ? Math.Round(g.Sum(e => e.Amount) / total * 100, 2) : 0
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();

        var byBranch = expenses
            .GroupBy(e => new { e.BranchId, BranchName = e.Branch?.Name ?? "(Unassigned)" })
            .Select(g => new ExpenseByBranchDto
            {
                BranchId = g.Key.BranchId,
                BranchName = g.Key.BranchName,
                TotalAmount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();

        var byMonth = expenses
            .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .Select(g => new ExpenseByMonthDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalAmount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        return new ExpensesReportDto
        {
            FromDate = from,
            ToDate = to,
            TotalExpenses = total,
            ExpensesCount = expenses.Count,
            ByCategory = byCategory,
            ByBranch = byBranch,
            ByMonth = byMonth
        };
    }
}

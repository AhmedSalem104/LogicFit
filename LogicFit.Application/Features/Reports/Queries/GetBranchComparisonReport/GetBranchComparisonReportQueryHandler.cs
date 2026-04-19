using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetBranchComparisonReport;

public class GetBranchComparisonReportQueryHandler : IRequestHandler<GetBranchComparisonReportQuery, BranchComparisonReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public GetBranchComparisonReportQueryHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<BranchComparisonReportDto> Handle(GetBranchComparisonReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;
        var from = request.FromDate ?? new DateTime(now.Year, now.Month, 1);
        var to = request.ToDate ?? now;

        var branches = await _context.Branches
            .Where(b => b.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var result = new List<BranchPerformanceDto>();

        foreach (var b in branches)
        {
            var activeMembers = await _context.ClientSubscriptions
                .CountAsync(s => s.BranchId == b.Id && s.TenantId == tenantId && s.Status == SubscriptionStatus.Active && s.EndDate >= now, cancellationToken);

            var checkIns = await _context.Attendances
                .CountAsync(a => a.BranchId == b.Id && a.TenantId == tenantId && a.CheckInTime >= from && a.CheckInTime <= to, cancellationToken);

            var subscriptionRevenue = await _context.Payments
                .Where(p => p.BranchId == b.Id && p.TenantId == tenantId && p.SubscriptionId != null && p.ReceivedAt >= from && p.ReceivedAt <= to)
                .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0;

            var posRevenue = await _context.Sales
                .Where(s => s.BranchId == b.Id && s.TenantId == tenantId && s.SaleDate >= from && s.SaleDate <= to)
                .SumAsync(s => (decimal?)s.Total, cancellationToken) ?? 0;

            var expenses = await _context.Expenses
                .Where(e => e.BranchId == b.Id && e.TenantId == tenantId && e.ExpenseDate >= from && e.ExpenseDate <= to)
                .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0;

            var classesHeld = await _context.ClassSchedules
                .CountAsync(s => s.GroupClass.BranchId == b.Id && s.TenantId == tenantId
                    && s.StartTime >= from && s.StartTime <= to && !s.IsCancelled, cancellationToken);

            var employees = await _context.EmployeeBranches
                .CountAsync(eb => eb.BranchId == b.Id && eb.TenantId == tenantId && eb.Employee.TerminationDate == null, cancellationToken);

            result.Add(new BranchPerformanceDto
            {
                BranchId = b.Id,
                BranchName = b.Name,
                ActiveMembers = activeMembers,
                CheckIns = checkIns,
                SubscriptionRevenue = subscriptionRevenue,
                PosRevenue = posRevenue,
                Expenses = expenses,
                ClassesHeld = classesHeld,
                Employees = employees
            });
        }

        return new BranchComparisonReportDto
        {
            FromDate = from,
            ToDate = to,
            Branches = result.OrderByDescending(b => b.TotalRevenue).ToList()
        };
    }
}

using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetOperationsDashboard;

public class GetOperationsDashboardQueryHandler : IRequestHandler<GetOperationsDashboardQuery, OperationsDashboardDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public GetOperationsDashboardQueryHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<OperationsDashboardDto> Handle(GetOperationsDashboardQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var weekLater = now.AddDays(7);
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var activeMembers = await _context.ClientSubscriptions
            .CountAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active && s.EndDate >= now, cancellationToken);

        var todayCheckIns = await _context.Attendances
            .CountAsync(a => a.TenantId == tenantId && a.CheckInTime >= today && a.CheckInTime < tomorrow, cancellationToken);

        var currentlyInside = await _context.Attendances
            .CountAsync(a => a.TenantId == tenantId && a.CheckOutTime == null, cancellationToken);

        var expiringIn7Days = await _context.ClientSubscriptions
            .CountAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active
                && s.EndDate >= now && s.EndDate <= weekLater, cancellationToken);

        var expired = await _context.ClientSubscriptions
            .CountAsync(s => s.TenantId == tenantId && s.EndDate < now
                && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Expired), cancellationToken);

        var monthRevenue = await _context.Payments
            .Where(p => p.TenantId == tenantId && p.ReceivedAt >= monthStart && p.ReceivedAt < monthEnd)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0;

        var monthExpenses = await _context.Expenses
            .Where(e => e.TenantId == tenantId && e.ExpenseDate >= monthStart && e.ExpenseDate < monthEnd)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0;

        var todayRevenue = await _context.Payments
            .Where(p => p.TenantId == tenantId && p.ReceivedAt >= today && p.ReceivedAt < tomorrow)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0;

        var todayExpenses = await _context.Expenses
            .Where(e => e.TenantId == tenantId && e.ExpenseDate >= today && e.ExpenseDate < tomorrow)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0;

        var lowStock = await _context.StockItems
            .Include(s => s.Product)
            .Where(s => s.TenantId == tenantId && s.Product.TrackStock && s.Quantity <= s.Product.MinStockLevel)
            .CountAsync(cancellationToken);

        var underMaintenance = await _context.Equipment
            .CountAsync(e => e.TenantId == tenantId && e.Status == EquipmentStatus.UnderMaintenance, cancellationToken);

        var pendingLeaves = await _context.LeaveRequests
            .CountAsync(l => l.TenantId == tenantId && l.Status == LeaveStatus.Pending, cancellationToken);

        var unpaidInvoices = await _context.Invoices
            .Where(i => i.TenantId == tenantId && (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue))
            .ToListAsync(cancellationToken);

        var branches = await _context.Branches
            .Where(b => b.TenantId == tenantId && b.IsActive)
            .ToListAsync(cancellationToken);

        var branchKpis = new List<BranchKpiDto>();
        foreach (var b in branches)
        {
            var insideCount = await _context.Attendances
                .CountAsync(a => a.BranchId == b.Id && a.TenantId == tenantId && a.CheckOutTime == null, cancellationToken);
            var todayInB = await _context.Attendances
                .CountAsync(a => a.BranchId == b.Id && a.TenantId == tenantId && a.CheckInTime >= today && a.CheckInTime < tomorrow, cancellationToken);
            var activeInB = await _context.ClientSubscriptions
                .CountAsync(s => s.BranchId == b.Id && s.TenantId == tenantId && s.Status == SubscriptionStatus.Active && s.EndDate >= now, cancellationToken);

            branchKpis.Add(new BranchKpiDto
            {
                BranchId = b.Id,
                BranchName = b.Name,
                Capacity = b.Capacity,
                CurrentlyInside = insideCount,
                TodayCheckIns = todayInB,
                ActiveMembers = activeInB
            });
        }

        return new OperationsDashboardDto
        {
            ActiveMembers = activeMembers,
            TodayCheckIns = todayCheckIns,
            CurrentlyInsideCount = currentlyInside,
            ExpiringSubscriptionsIn7Days = expiringIn7Days,
            ExpiredSubscriptions = expired,
            MonthRevenue = monthRevenue,
            MonthExpenses = monthExpenses,
            TodayRevenue = todayRevenue,
            TodayExpenses = todayExpenses,
            LowStockProductsCount = lowStock,
            EquipmentUnderMaintenanceCount = underMaintenance,
            PendingLeaveRequestsCount = pendingLeaves,
            UnpaidInvoicesCount = unpaidInvoices.Count,
            UnpaidInvoicesTotal = unpaidInvoices.Sum(i => i.RemainingAmount),
            BranchKpis = branchKpis
        };
    }
}

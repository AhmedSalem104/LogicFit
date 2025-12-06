using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetSubscriptionsReport;

public class GetSubscriptionsReportQueryHandler : IRequestHandler<GetSubscriptionsReportQuery, SubscriptionsReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetSubscriptionsReportQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<SubscriptionsReportDto> Handle(GetSubscriptionsReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var in7Days = now.AddDays(7);
        var in30Days = now.AddDays(30);

        var totalSubscriptions = await _context.ClientSubscriptions
            .CountAsync(cs => cs.TenantId == tenantId && !cs.IsDeleted, cancellationToken);

        var activeSubscriptions = await _context.ClientSubscriptions
            .CountAsync(cs => cs.TenantId == tenantId && cs.Status == SubscriptionStatus.Active && !cs.IsDeleted, cancellationToken);

        var expiredSubscriptions = await _context.ClientSubscriptions
            .CountAsync(cs => cs.TenantId == tenantId && cs.Status == SubscriptionStatus.Expired && !cs.IsDeleted, cancellationToken);

        var cancelledSubscriptions = await _context.ClientSubscriptions
            .CountAsync(cs => cs.TenantId == tenantId && cs.Status == SubscriptionStatus.Cancelled && !cs.IsDeleted, cancellationToken);

        var expiringIn7Days = await _context.ClientSubscriptions
            .CountAsync(cs => cs.TenantId == tenantId && cs.Status == SubscriptionStatus.Active &&
                        cs.EndDate <= in7Days && cs.EndDate > now && !cs.IsDeleted, cancellationToken);

        var expiringIn30Days = await _context.ClientSubscriptions
            .CountAsync(cs => cs.TenantId == tenantId && cs.Status == SubscriptionStatus.Active &&
                        cs.EndDate <= in30Days && cs.EndDate > now && !cs.IsDeleted, cancellationToken);

        // Total revenue
        var allSubscriptions = await _context.ClientSubscriptions
            .Include(cs => cs.Plan)
            .Where(cs => cs.TenantId == tenantId && !cs.IsDeleted)
            .ToListAsync(cancellationToken);
        var totalRevenue = allSubscriptions.Sum(cs => cs.Plan?.Price ?? 0);

        var subscriptionsThisMonth = await _context.ClientSubscriptions
            .Include(cs => cs.Plan)
            .Where(cs => cs.TenantId == tenantId && cs.StartDate >= startOfMonth && !cs.IsDeleted)
            .ToListAsync(cancellationToken);
        var revenueThisMonth = subscriptionsThisMonth.Sum(cs => cs.Plan?.Price ?? 0);

        // Plan statistics
        var planStats = await _context.SubscriptionPlans
            .Include(sp => sp.Subscriptions)
            .Where(sp => sp.TenantId == tenantId && !sp.IsDeleted)
            .Select(sp => new SubscriptionPlanStatsDto
            {
                PlanId = sp.Id,
                PlanName = sp.Name,
                ActiveCount = sp.Subscriptions.Count(cs => cs.Status == SubscriptionStatus.Active && !cs.IsDeleted),
                TotalSold = sp.Subscriptions.Count(cs => !cs.IsDeleted),
                TotalRevenue = sp.Subscriptions.Count(cs => !cs.IsDeleted) * sp.Price
            })
            .ToListAsync(cancellationToken);

        // Monthly revenue (last 6 months)
        var monthlyRevenue = new List<MonthlyRevenueDto>();
        for (int i = 5; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);

            var monthSubs = await _context.ClientSubscriptions
                .Include(cs => cs.Plan)
                .Where(cs => cs.TenantId == tenantId && cs.StartDate >= monthStart && cs.StartDate < monthEnd && !cs.IsDeleted)
                .ToListAsync(cancellationToken);

            var revenue = monthSubs.Sum(cs => cs.Plan?.Price ?? 0);
            var count = monthSubs.Count;

            monthlyRevenue.Add(new MonthlyRevenueDto
            {
                Month = monthStart.ToString("MMM yyyy"),
                Revenue = revenue,
                SubscriptionCount = count
            });
        }

        return new SubscriptionsReportDto
        {
            TotalSubscriptions = totalSubscriptions,
            ActiveSubscriptions = activeSubscriptions,
            ExpiredSubscriptions = expiredSubscriptions,
            CancelledSubscriptions = cancelledSubscriptions,
            ExpiringIn7Days = expiringIn7Days,
            ExpiringIn30Days = expiringIn30Days,
            TotalRevenue = totalRevenue,
            RevenueThisMonth = revenueThisMonth,
            PlanStatistics = planStats,
            MonthlyRevenue = monthlyRevenue
        };
    }
}

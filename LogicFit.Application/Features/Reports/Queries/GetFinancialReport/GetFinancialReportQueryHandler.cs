using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetFinancialReport;

public class GetFinancialReportQueryHandler : IRequestHandler<GetFinancialReportQuery, FinancialReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetFinancialReportQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<FinancialReportDto> Handle(GetFinancialReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);

        // Get all subscriptions with their plans
        var allSubscriptions = await _context.ClientSubscriptions
            .Include(cs => cs.Plan)
            .Where(cs => cs.TenantId == tenantId && !cs.IsDeleted)
            .ToListAsync(cancellationToken);

        var totalRevenue = allSubscriptions.Sum(cs => cs.Plan?.Price ?? 0);

        var subscriptionsThisMonth = allSubscriptions.Where(cs => cs.StartDate >= startOfMonth).ToList();
        var revenueThisMonth = subscriptionsThisMonth.Sum(cs => cs.Plan?.Price ?? 0);

        var subscriptionsLastMonth = allSubscriptions.Where(cs => cs.StartDate >= startOfLastMonth && cs.StartDate < startOfMonth).ToList();
        var revenueLastMonth = subscriptionsLastMonth.Sum(cs => cs.Plan?.Price ?? 0);

        var growthPercentage = revenueLastMonth > 0
            ? ((revenueThisMonth - revenueLastMonth) / revenueLastMonth) * 100
            : 0;

        var subscriptionCount = allSubscriptions.Count;
        var averageSubscriptionValue = subscriptionCount > 0 ? totalRevenue / subscriptionCount : 0;

        var totalWalletBalance = await _context.Users
            .Where(u => u.TenantId == tenantId && u.Role == UserRole.Client && !u.IsDeleted)
            .SumAsync(u => u.WalletBalance, cancellationToken);

        // Monthly revenue (last 12 months)
        var monthlyRevenue = new List<MonthlyRevenueDto>();
        for (int i = 11; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);

            var monthSubs = allSubscriptions.Where(cs => cs.StartDate >= monthStart && cs.StartDate < monthEnd).ToList();
            var revenue = monthSubs.Sum(cs => cs.Plan?.Price ?? 0);
            var count = monthSubs.Count;

            monthlyRevenue.Add(new MonthlyRevenueDto
            {
                Month = monthStart.ToString("MMM yyyy"),
                Revenue = revenue,
                SubscriptionCount = count
            });
        }

        // Group by subscription plan (as a proxy for payment type since there's no PaymentMethod)
        var paymentMethods = allSubscriptions
            .GroupBy(cs => cs.Plan?.Name ?? "Unknown")
            .Select(g => new PaymentMethodStatsDto
            {
                PaymentMethod = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(cs => cs.Plan?.Price ?? 0)
            })
            .ToList();

        return new FinancialReportDto
        {
            TotalRevenue = totalRevenue,
            RevenueThisMonth = revenueThisMonth,
            RevenueLastMonth = revenueLastMonth,
            GrowthPercentage = growthPercentage,
            AverageSubscriptionValue = averageSubscriptionValue,
            TotalWalletBalance = totalWalletBalance,
            MonthlyRevenue = monthlyRevenue,
            PaymentMethods = paymentMethods
        };
    }
}

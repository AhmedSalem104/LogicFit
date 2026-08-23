using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Application.Features.Reports.Services;
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

        var refundsBySubscription = await _context.WalletTransactions
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId &&
                        t.Type == TransactionType.Refund &&
                        t.ReferenceType == SubscriptionRevenueCalculator.SubscriptionReferenceType &&
                        t.ReferenceId.HasValue)
            .GroupBy(t => t.ReferenceId!.Value)
            .Select(g => new { SubscriptionId = g.Key, Amount = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.SubscriptionId, x => x.Amount, cancellationToken);

        decimal GetRevenue(Domain.Entities.ClientSubscription subscription)
            => SubscriptionRevenueCalculator.NetCollectedAmount(
                subscription,
                refundsBySubscription.GetValueOrDefault(subscription.Id));

        var totalRevenue = allSubscriptions.Sum(GetRevenue);

        var subscriptionsThisMonth = allSubscriptions.Where(cs => cs.StartDate >= startOfMonth).ToList();
        var revenueThisMonth = subscriptionsThisMonth.Sum(GetRevenue);

        var subscriptionsLastMonth = allSubscriptions.Where(cs => cs.StartDate >= startOfLastMonth && cs.StartDate < startOfMonth).ToList();
        var revenueLastMonth = subscriptionsLastMonth.Sum(GetRevenue);

        // TOP GYM's daily visitors are completed sales, not subscriptions. Keep their
        // source explicit and add them to collected revenue only after the sale is completed.
        var dayPasses = await _context.DayPassSales
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.Status == DayPassStatus.Completed)
            .Select(x => new { x.VisitDate, x.AmountPaid })
            .ToListAsync(cancellationToken);
        var dayPassRevenue = dayPasses.Sum(x => x.AmountPaid);
        totalRevenue += dayPassRevenue;
        var dayPassesThisMonth = dayPasses.Where(x => x.VisitDate >= startOfMonth && x.VisitDate < startOfMonth.AddMonths(1)).ToList();
        var dayPassesLastMonth = dayPasses.Where(x => x.VisitDate >= startOfLastMonth && x.VisitDate < startOfMonth).ToList();
        revenueThisMonth += dayPassesThisMonth.Sum(x => x.AmountPaid);
        revenueLastMonth += dayPassesLastMonth.Sum(x => x.AmountPaid);

        var growthPercentage = revenueLastMonth > 0
            ? ((revenueThisMonth - revenueLastMonth) / revenueLastMonth) * 100
            : 0;

        var subscriptionCount = allSubscriptions.Count;
        var averageSubscriptionValue = subscriptionCount > 0 ? (totalRevenue - dayPassRevenue) / subscriptionCount : 0;

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
            var revenue = monthSubs.Sum(GetRevenue);
            var count = monthSubs.Count;
            var monthDayPasses = dayPasses.Where(x => x.VisitDate >= monthStart && x.VisitDate < monthEnd).ToList();
            revenue += monthDayPasses.Sum(x => x.AmountPaid);

            monthlyRevenue.Add(new MonthlyRevenueDto
            {
                Month = monthStart.ToString("MMM yyyy"),
                Revenue = revenue,
                SubscriptionCount = count,
                DayPassCount = monthDayPasses.Count
            });
        }

        // Group by subscription plan (as a proxy for payment type since there's no PaymentMethod)
        var paymentMethods = allSubscriptions
            .GroupBy(cs => cs.Plan?.Name ?? "Unknown")
            .Select(g => new PaymentMethodStatsDto
            {
                PaymentMethod = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(GetRevenue)
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
            DayPassRevenue = dayPassRevenue,
            DayPassCount = dayPasses.Count,
            MonthlyRevenue = monthlyRevenue,
            PaymentMethods = paymentMethods
        };
    }
}

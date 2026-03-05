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

        var allSubscriptions = await _context.ClientSubscriptions
            .Include(cs => cs.Plan)
            .Where(cs => cs.TenantId == tenantId && !cs.IsDeleted)
            .ToListAsync(cancellationToken);

        var totalSubscriptions = allSubscriptions.Count;
        var activeSubscriptions = allSubscriptions.Count(cs => cs.Status == SubscriptionStatus.Active);
        var suspendedSubscriptions = allSubscriptions.Count(cs => cs.Status == SubscriptionStatus.Suspended);
        var trialSubscriptions = allSubscriptions.Count(cs => cs.Status == SubscriptionStatus.Trial);
        var expiredSubscriptions = allSubscriptions.Count(cs => cs.Status == SubscriptionStatus.Expired);
        var cancelledSubscriptions = allSubscriptions.Count(cs => cs.Status == SubscriptionStatus.Cancelled);

        var expiringIn7Days = allSubscriptions.Count(cs =>
            cs.Status == SubscriptionStatus.Active && cs.EndDate <= in7Days && cs.EndDate > now);
        var expiringIn30Days = allSubscriptions.Count(cs =>
            cs.Status == SubscriptionStatus.Active && cs.EndDate <= in30Days && cs.EndDate > now);

        // Revenue: use AmountPaid if > 0, otherwise fallback to Plan.Price
        decimal GetRevenue(Domain.Entities.ClientSubscription cs) =>
            cs.AmountPaid > 0 ? cs.AmountPaid : (cs.Plan?.Price ?? 0);

        var totalRevenue = allSubscriptions.Sum(GetRevenue);
        var revenueThisMonth = allSubscriptions
            .Where(cs => cs.StartDate >= startOfMonth)
            .Sum(GetRevenue);

        // Renewal rate
        var renewedCount = allSubscriptions.Count(cs => cs.RenewedFromId != null);
        var finishedCount = allSubscriptions.Count(cs =>
            cs.Status == SubscriptionStatus.Expired || cs.Status == SubscriptionStatus.Cancelled);
        var renewalRate = finishedCount > 0 ? Math.Round((decimal)renewedCount / finishedCount * 100, 1) : 0;

        // Average subscription duration
        var avgDuration = allSubscriptions.Count > 0
            ? allSubscriptions.Average(cs => (cs.EndDate - cs.StartDate).TotalDays)
            : 0;

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
                TotalRevenue = sp.Subscriptions
                    .Where(cs => !cs.IsDeleted)
                    .Sum(cs => cs.AmountPaid > 0 ? cs.AmountPaid : sp.Price)
            })
            .ToListAsync(cancellationToken);

        // Monthly revenue (last 6 months)
        var monthlyRevenue = new List<MonthlyRevenueDto>();
        for (int i = 5; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);

            var monthSubs = allSubscriptions
                .Where(cs => cs.StartDate >= monthStart && cs.StartDate < monthEnd)
                .ToList();

            monthlyRevenue.Add(new MonthlyRevenueDto
            {
                Month = monthStart.ToString("MMM yyyy"),
                Revenue = monthSubs.Sum(GetRevenue),
                SubscriptionCount = monthSubs.Count
            });
        }

        // Revenue by payment method
        var revenueByPayment = allSubscriptions
            .Where(cs => cs.PaymentMethod.HasValue)
            .GroupBy(cs => cs.PaymentMethod!.Value)
            .Select(g => new RevenueByPaymentMethodDto
            {
                PaymentMethod = g.Key.ToString(),
                Count = g.Count(),
                TotalRevenue = g.Sum(cs => cs.AmountPaid)
            })
            .ToList();

        return new SubscriptionsReportDto
        {
            TotalSubscriptions = totalSubscriptions,
            ActiveSubscriptions = activeSubscriptions,
            SuspendedSubscriptions = suspendedSubscriptions,
            TrialSubscriptions = trialSubscriptions,
            ExpiredSubscriptions = expiredSubscriptions,
            CancelledSubscriptions = cancelledSubscriptions,
            ExpiringIn7Days = expiringIn7Days,
            ExpiringIn30Days = expiringIn30Days,
            TotalRevenue = totalRevenue,
            RevenueThisMonth = revenueThisMonth,
            RenewalRate = renewalRate,
            AverageSubscriptionDurationDays = Math.Round(avgDuration, 1),
            PlanStatistics = planStats,
            MonthlyRevenue = monthlyRevenue,
            RevenueByPaymentMethod = revenueByPayment
        };
    }
}

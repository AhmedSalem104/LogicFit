using LogicFit.Application.Features.Reports.Services;
using LogicFit.Domain.Entities;
using Xunit;

namespace LogicFit.Tests;

public sealed class FinancialReportRevenueSourceTests
{
    [Theory]
    [InlineData(600, 0, 0)]
    [InlineData(600, 250, 250)]
    [InlineData(450, 450, 450)]
    public void Paid_revenue_uses_amount_paid_not_plan_price(
        decimal planPrice,
        decimal amountPaid,
        decimal expectedRevenue)
    {
        var subscription = new ClientSubscription
        {
            AmountPaid = amountPaid,
            TotalAmount = planPrice,
            Plan = new SubscriptionPlan { Price = planPrice }
        };

        Assert.Equal(expectedRevenue, SubscriptionRevenueCalculator.PaidAmount(subscription));
    }

    [Fact]
    public void All_subscription_report_handlers_use_the_shared_paid_revenue_source()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var handlers = new[]
        {
            Path.Combine(root, "LogicFit.Application", "Features", "Reports", "Queries", "GetFinancialReport", "GetFinancialReportQueryHandler.cs"),
            Path.Combine(root, "LogicFit.Application", "Features", "Reports", "Queries", "GetSubscriptionsReport", "GetSubscriptionsReportQueryHandler.cs"),
            Path.Combine(root, "LogicFit.Application", "Features", "Reports", "Queries", "GetDashboardReport", "GetDashboardReportQueryHandler.cs")
        };

        foreach (var handler in handlers)
        {
            var source = File.ReadAllText(handler);
            Assert.Contains("SubscriptionRevenueCalculator.PaidAmount", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Plan.Price", source, StringComparison.Ordinal);
        }
    }
}

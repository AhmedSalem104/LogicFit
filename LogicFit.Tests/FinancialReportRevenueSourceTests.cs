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

    [Theory]
    [InlineData(600, 0, 600, true)]
    [InlineData(600, 250, 350, true)]
    [InlineData(600, 600, 0, false)]
    [InlineData(600, 250, 351, false)]
    public void Remaining_balance_and_payment_guard_are_consistent(
        decimal totalAmount,
        decimal amountPaid,
        decimal attemptedAmount,
        bool expectedAllowed)
    {
        var subscription = new ClientSubscription { TotalAmount = totalAmount, AmountPaid = amountPaid };

        Assert.Equal(totalAmount - amountPaid, SubscriptionRevenueCalculator.RemainingAmount(subscription));
        Assert.Equal(expectedAllowed, SubscriptionRevenueCalculator.CanApplyPayment(subscription, attemptedAmount));
    }

    [Fact]
    public void All_subscription_report_handlers_use_the_shared_paid_revenue_source()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var handlers = new[]
        {
            Path.Combine(root, "LogicFit.Application", "Features", "Reports", "Queries", "GetFinancialReport", "GetFinancialReportQueryHandler.cs"),
            Path.Combine(root, "LogicFit.Application", "Features", "Reports", "Queries", "GetSubscriptionsReport", "GetSubscriptionsReportQueryHandler.cs"),
            Path.Combine(root, "LogicFit.Application", "Features", "Reports", "Queries", "GetDashboardReport", "GetDashboardReportQueryHandler.cs"),
            Path.Combine(root, "LogicFit.Application", "Features", "Reports", "Queries", "GetClientsReport", "GetClientsReportQueryHandler.cs")
        };

        foreach (var handler in handlers)
        {
            var source = File.ReadAllText(handler);
            if (source.Contains("GetClientsReportQueryHandler", StringComparison.Ordinal))
                Assert.Contains("Sum(s => s.AmountPaid)", source, StringComparison.Ordinal);
            else
                Assert.Contains("SubscriptionRevenueCalculator.PaidAmount", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Plan.Price", source, StringComparison.Ordinal);
        }

        var recordPaymentSource = File.ReadAllText(Path.Combine(root, "LogicFit.Application", "Features", "Payments", "Commands", "RecordPayment", "RecordPaymentCommandHandler.cs"));
        Assert.Contains("SubscriptionRevenueCalculator.CanApplyPayment", recordPaymentSource, StringComparison.Ordinal);
        Assert.Contains("ClientSubscription", recordPaymentSource, StringComparison.Ordinal);
    }
}

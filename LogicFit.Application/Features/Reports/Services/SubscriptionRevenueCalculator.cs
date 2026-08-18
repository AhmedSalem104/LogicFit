using LogicFit.Domain.Entities;

namespace LogicFit.Application.Features.Reports.Services;

/// <summary>
/// Defines the collected-revenue source for subscription reports.
/// Plan.Price and TotalAmount describe expected/contract values; AmountPaid is the
/// persisted aggregate for money actually collected for the subscription.
/// </summary>
public static class SubscriptionRevenueCalculator
{
    public const string SubscriptionReferenceType = "Subscription";

    public static decimal PaidAmount(ClientSubscription subscription) => subscription.AmountPaid;

    public static decimal NetCollectedAmount(ClientSubscription subscription, decimal refundedAmount)
    {
        var boundedRefund = Math.Min(Math.Max(0m, refundedAmount), Math.Max(0m, subscription.AmountPaid));
        return Math.Max(0m, subscription.AmountPaid - boundedRefund);
    }

    public static decimal RemainingAmount(ClientSubscription subscription) =>
        Math.Max(0m, subscription.TotalAmount - subscription.AmountPaid);

    public static bool CanApplyPayment(ClientSubscription subscription, decimal amount) =>
        amount > 0m && amount <= RemainingAmount(subscription);
}

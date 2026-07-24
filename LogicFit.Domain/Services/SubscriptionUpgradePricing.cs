namespace LogicFit.Domain.Services;

public static class SubscriptionUpgradePricing
{
    public static decimal Calculate(decimal currentPrice, decimal targetPrice, int remainingDays, int currentDurationDays)
    {
        if (remainingDays <= 0 || currentDurationDays <= 0 || targetPrice <= currentPrice) return 0m;
        var difference = targetPrice - currentPrice;
        var prorated = difference * remainingDays / currentDurationDays;
        return decimal.Round(prorated, 2, MidpointRounding.AwayFromZero);
    }
}

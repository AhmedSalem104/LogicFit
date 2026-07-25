using LogicFit.Domain.Services;
using Xunit;

namespace LogicFit.Tests;

public class SubscriptionUpgradePricingTests
{
    [Fact]
    public void Calculates_prorated_difference()
        => Assert.Equal(500m, SubscriptionUpgradePricing.Calculate(1000m, 2000m, 15, 30));

    [Fact]
    public void Never_charges_for_downgrade()
        => Assert.Equal(0m, SubscriptionUpgradePricing.Calculate(2000m, 1000m, 15, 30));
}

using Xunit;

namespace LogicFit.Tests;

public sealed class SubscriptionOnboardingContractTests
{
    [Fact]
    public void New_client_onboarding_joins_the_outer_transaction_for_membership_creation()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var onboarding = File.ReadAllText(Path.Combine(
            root,
            "LogicFit.Application",
            "Features",
            "Clients",
            "Commands",
            "OnboardClient",
            "OnboardClientCommandHandler.cs"));
        var subscription = File.ReadAllText(Path.Combine(
            root,
            "LogicFit.Application",
            "Features",
            "Subscriptions",
            "Commands",
            "CreateClientSubscription",
            "CreateClientSubscriptionCommandHandler.cs"));

        Assert.Contains("UseExistingTransaction = true", onboarding, StringComparison.Ordinal);
        Assert.Contains("request.UseExistingTransaction", subscription, StringComparison.Ordinal);
        Assert.Contains("if (dbTransaction is not null)", subscription, StringComparison.Ordinal);
        Assert.Contains("RollbackAsync", subscription, StringComparison.Ordinal);
    }
}

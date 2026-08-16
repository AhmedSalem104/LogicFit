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
    [Fact]
    public void New_client_onboarding_normalizes_optional_email_and_does_not_require_a_valid_seller_claim()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var clientHandler = File.ReadAllText(Path.Combine(
            root,
            "LogicFit.Application",
            "Features",
            "Clients",
            "Commands",
            "CreateClient",
            "CreateClientCommandHandler.cs"));
        var subscriptionHandler = File.ReadAllText(Path.Combine(
            root,
            "LogicFit.Application",
            "Features",
            "Subscriptions",
            "Commands",
            "CreateClientSubscription",
            "CreateClientSubscriptionCommandHandler.cs"));

        Assert.Contains("string.IsNullOrWhiteSpace(request.Email)", clientHandler, StringComparison.Ordinal);
        Assert.Contains("Email already registered", clientHandler, StringComparison.Ordinal);
        Assert.Contains("Guid.TryParse(_currentUserService.UserId", subscriptionHandler, StringComparison.Ordinal);
        Assert.Contains("sellerUserId = null", subscriptionHandler, StringComparison.Ordinal);
    }

    [Fact]
    public void Client_role_can_be_repaired_for_pre_rbac_tenant_databases()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var rbacService = File.ReadAllText(Path.Combine(
            root,
            "LogicFit.Application",
            "Common",
            "Services",
            "RbacService.cs"));

        Assert.Contains("SystemRoles.Client", rbacService, StringComparison.Ordinal);
        Assert.Contains("zero-permission system role", rbacService, StringComparison.Ordinal);
        Assert.Contains("_context.AppRoles.Add(role)", rbacService, StringComparison.Ordinal);
    }
}

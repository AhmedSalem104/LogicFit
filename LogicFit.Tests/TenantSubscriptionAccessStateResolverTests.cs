using LogicFit.Application.Common.Services;
using LogicFit.Domain.Enums;
using Xunit;

namespace LogicFit.Tests;

public sealed class TenantSubscriptionAccessStateResolverTests
{
    private static readonly DateTime Now = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Cancelled_subscription_keeps_full_access_until_its_end_date()
    {
        var effective = TenantSubscriptionAccessStateResolver.Resolve(
            TenantSubscriptionStatus.Cancelled,
            Now.AddMinutes(1),
            trialEndsAt: null,
            Now);

        Assert.Equal(TenantSubscriptionStatus.Active, effective);
    }

    [Fact]
    public void Cancelled_subscription_is_effectively_expired_at_its_end_date()
    {
        var effective = TenantSubscriptionAccessStateResolver.Resolve(
            TenantSubscriptionStatus.Cancelled,
            Now,
            trialEndsAt: null,
            Now);

        Assert.Equal(TenantSubscriptionStatus.Expired, effective);
    }

    [Fact]
    public void Trial_is_effectively_expired_at_trial_end()
    {
        var effective = TenantSubscriptionAccessStateResolver.Resolve(
            TenantSubscriptionStatus.Trial,
            endDate: null,
            trialEndsAt: Now,
            Now);

        Assert.Equal(TenantSubscriptionStatus.Expired, effective);
    }
}

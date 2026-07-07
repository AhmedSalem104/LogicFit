using LogicFit.Domain.Enums;
using Xunit;

namespace LogicFit.Tests;

/// <summary>
/// TenantStatus intentionally preserves the legacy SubscriptionStatus int values that were stored
/// in Tenant.Status, so upgrading needs no data remap. If these break, add a data migration.
/// </summary>
public class TenantStatusInvariantTests
{
    [Theory]
    [InlineData(TenantStatus.Active, 1)]
    [InlineData(TenantStatus.Suspended, 2)]
    [InlineData(TenantStatus.Trial, 3)]
    [InlineData(TenantStatus.PastDue, 4)]
    [InlineData(TenantStatus.Cancelled, 5)]
    public void TenantStatus_Preserves_Legacy_SubscriptionStatus_Values(TenantStatus status, int expected)
    {
        Assert.Equal(expected, (int)status);
    }

    [Fact]
    public void SubscriptionStatus_Legacy_Values_Are_Unchanged()
    {
        Assert.Equal(1, (int)SubscriptionStatus.Active);
        Assert.Equal(2, (int)SubscriptionStatus.Suspended);
        Assert.Equal(3, (int)SubscriptionStatus.Trial);
        Assert.Equal(4, (int)SubscriptionStatus.Expired);
        Assert.Equal(5, (int)SubscriptionStatus.Cancelled);
    }
}

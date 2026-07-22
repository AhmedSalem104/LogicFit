using LogicFit.Application.Common.Services;
using LogicFit.Domain.Enums;
using Xunit;

namespace LogicFit.Tests;

/// <summary>
/// Locks in the pure tenant-access decision rules (no DB). Covers the gym-status × subscription-status ×
/// suspension-reason matrix that both gates and the authorization handler rely on.
/// </summary>
public class TenantAccessPolicyTests
{
    private static TenantAccessState State(
        TenantStatus tenant,
        TenantSubscriptionStatus? sub = null,
        SuspensionReason? reason = null,
        bool exists = true)
        => new(exists, tenant, sub, reason);

    [Theory]
    [InlineData(TenantStatus.Active)]
    [InlineData(TenantStatus.Trial)]
    [InlineData(TenantStatus.PastDue)]
    public void Operational_Statuses_Are_Not_Blocked_And_Not_Pending(TenantStatus status)
    {
        var s = State(status);
        Assert.Null(TenantAccessPolicy.EvaluateHardBlock(s));
        Assert.False(TenantAccessPolicy.IsPendingApproval(s));
    }

    [Fact]
    public void PendingApproval_Is_Not_HardBlocked_But_Is_Pending()
    {
        var s = State(TenantStatus.PendingApproval);
        Assert.Null(TenantAccessPolicy.EvaluateHardBlock(s));
        Assert.True(TenantAccessPolicy.IsPendingApproval(s));
    }

    [Fact]
    public void Suspended_NonPayment_Blocks_402_With_NonPayment_Code()
    {
        var b = TenantAccessPolicy.EvaluateHardBlock(State(TenantStatus.Suspended, reason: SuspensionReason.NonPayment));
        Assert.NotNull(b);
        Assert.Equal("TENANT_SUSPENDED_NONPAYMENT", b!.Code);
        Assert.Equal(402, b.HttpStatus);
    }

    [Fact]
    public void Suspended_ManualByAdmin_Blocks_403_Suspended()
    {
        var b = TenantAccessPolicy.EvaluateHardBlock(State(TenantStatus.Suspended, reason: SuspensionReason.ManualByAdmin));
        Assert.NotNull(b);
        Assert.Equal("TENANT_SUSPENDED", b!.Code);
        Assert.Equal(403, b.HttpStatus);
    }

    [Theory]
    [InlineData(TenantStatus.Cancelled, "TENANT_SUBSCRIPTION_CANCELLED", 402)]
    [InlineData(TenantStatus.Archived, "TENANT_ARCHIVED", 403)]
    [InlineData(TenantStatus.Deleted, "TENANT_NOT_FOUND", 404)]
    public void Terminal_Tenant_Statuses_Block_With_Expected_Code(TenantStatus status, string code, int http)
    {
        var b = TenantAccessPolicy.EvaluateHardBlock(State(status));
        Assert.NotNull(b);
        Assert.Equal(code, b!.Code);
        Assert.Equal(http, b.HttpStatus);
    }

    [Fact]
    public void Missing_Tenant_Is_NotFound()
    {
        var b = TenantAccessPolicy.EvaluateHardBlock(State(TenantStatus.Deleted, exists: false));
        Assert.NotNull(b);
        Assert.Equal("TENANT_NOT_FOUND", b!.Code);
        Assert.Equal(404, b.HttpStatus);
    }

    [Theory]
    [InlineData(TenantSubscriptionStatus.Expired, "TENANT_SUBSCRIPTION_EXPIRED", 402)]
    [InlineData(TenantSubscriptionStatus.Cancelled, "TENANT_SUBSCRIPTION_CANCELLED", 402)]
    [InlineData(TenantSubscriptionStatus.Suspended, "TENANT_SUBSCRIPTION_SUSPENDED", 402)]
    public void Subscription_Blocks_Even_When_Tenant_Status_Is_Active(TenantSubscriptionStatus sub, string code, int http)
    {
        // A gym still marked Active but whose subscription lapsed must be blocked with the precise code.
        var b = TenantAccessPolicy.EvaluateHardBlock(State(TenantStatus.Active, sub: sub));
        Assert.NotNull(b);
        Assert.Equal(code, b!.Code);
        Assert.Equal(http, b.HttpStatus);
    }

    [Theory]
    [InlineData(TenantSubscriptionStatus.Active)]
    [InlineData(TenantSubscriptionStatus.Trial)]
    [InlineData(TenantSubscriptionStatus.PastDue)]
    [InlineData(TenantSubscriptionStatus.PendingPayment)]
    public void Healthy_Or_Grace_Subscriptions_Do_Not_Block_An_Active_Tenant(TenantSubscriptionStatus sub)
    {
        Assert.Null(TenantAccessPolicy.EvaluateHardBlock(State(TenantStatus.Active, sub: sub)));
    }
}

using LogicFit.Application.Common.Services;
using LogicFit.Domain.Enums;
using Xunit;

namespace LogicFit.Tests;

public class TenantAccessPolicyTests
{
    private static TenantAccessState State(
        TenantStatus workspace = TenantStatus.Active,
        TenantSubscriptionStatus? subscription = TenantSubscriptionStatus.Active,
        SuspensionReason? reason = null,
        bool exists = true)
        => new(exists, workspace, subscription, reason);

    [Theory]
    [InlineData(TenantSubscriptionStatus.Trial)]
    [InlineData(TenantSubscriptionStatus.Active)]
    [InlineData(TenantSubscriptionStatus.PastDue)]
    [InlineData(TenantSubscriptionStatus.GracePeriod)]
    public void Healthy_or_grace_subscription_has_full_access(TenantSubscriptionStatus subscription)
    {
        Assert.Equal(TenantAccessMode.Full, TenantAccessPolicy.Evaluate(State(subscription: subscription)).Mode);
    }

    [Theory]
    [InlineData(TenantSubscriptionStatus.None)]
    [InlineData(TenantSubscriptionStatus.PendingPayment)]
    public void No_selected_plan_is_billing_only(TenantSubscriptionStatus subscription)
    {
        Assert.Equal(TenantAccessMode.BillingOnly, TenantAccessPolicy.Evaluate(State(subscription: subscription)).Mode);
    }

    [Theory]
    [InlineData(TenantSubscriptionStatus.Expired)]
    [InlineData(TenantSubscriptionStatus.Cancelled)]
    [InlineData(TenantSubscriptionStatus.Suspended)]
    public void Ended_subscription_is_read_only_not_hard_blocked(TenantSubscriptionStatus subscription)
    {
        var decision = TenantAccessPolicy.Evaluate(State(subscription: subscription));
        Assert.Equal(TenantAccessMode.ReadOnly, decision.Mode);
        Assert.Null(decision.Block);
    }

    [Fact]
    public void Legacy_gym_without_subscription_record_remains_operational_during_rollout()
    {
        Assert.Equal(TenantAccessMode.Full, TenantAccessPolicy.Evaluate(State(subscription: null)).Mode);
    }

    [Fact]
    public void New_freelance_workspace_without_subscription_is_billing_only()
    {
        var state = State(subscription: null) with { WorkspaceType = WorkspaceType.FreelanceCoach };
        Assert.Equal(TenantAccessMode.BillingOnly, TenantAccessPolicy.Evaluate(state).Mode);
    }

    [Fact]
    public void Pending_workspace_is_billing_only()
    {
        Assert.Equal(TenantAccessMode.BillingOnly, TenantAccessPolicy.Evaluate(State(TenantStatus.PendingApproval)).Mode);
    }

    [Theory]
    [InlineData(TenantStatus.Archived, "TENANT_ARCHIVED", 403)]
    [InlineData(TenantStatus.Provisioning, "WORKSPACE_PROVISIONING", 423)]
    [InlineData(TenantStatus.ProvisioningFailed, "WORKSPACE_PROVISIONING_FAILED", 503)]
    public void Non_operational_workspace_blocks_before_subscription(TenantStatus workspace, string code, int http)
    {
        var decision = TenantAccessPolicy.Evaluate(State(workspace, TenantSubscriptionStatus.Active));
        Assert.Equal(TenantAccessMode.Blocked, decision.Mode);
        Assert.Equal(code, decision.Block!.Code);
        Assert.Equal(http, decision.Block.HttpStatus);
    }

    [Fact]
    public void Suspended_workspace_wins_over_active_subscription()
    {
        var decision = TenantAccessPolicy.Evaluate(State(TenantStatus.Suspended, TenantSubscriptionStatus.Active, SuspensionReason.ManualByAdmin));
        Assert.Equal(TenantAccessMode.Blocked, decision.Mode);
        Assert.Equal("TENANT_SUSPENDED", decision.Block!.Code);
    }

    [Fact]
    public void Missing_workspace_is_not_found()
    {
        var decision = TenantAccessPolicy.Evaluate(State(exists: false));
        Assert.Equal(TenantAccessMode.Blocked, decision.Mode);
        Assert.Equal("TENANT_NOT_FOUND", decision.Block!.Code);
        Assert.Equal(404, decision.Block.HttpStatus);
    }
}

using LogicFit.Domain.Enums;
using LogicFit.Application.Common.Services;
using LogicFit.Domain.Services;
using Xunit;

namespace LogicFit.Tests;

public sealed class ProvisioningSagaContractTests
{
    [Fact]
    public void Provisioning_job_states_include_capacity_wait_and_retry_failure()
    {
        Assert.Contains(ProvisioningJobStatus.AwaitingDatabaseCapacity, Enum.GetValues<ProvisioningJobStatus>());
        Assert.Contains(ProvisioningJobStatus.Failed, Enum.GetValues<ProvisioningJobStatus>());
        Assert.Contains(ProvisioningJobStatus.Completed, Enum.GetValues<ProvisioningJobStatus>());
    }

    [Fact]
    public void Pending_activation_can_only_be_reached_before_term_starts()
    {
        Assert.True(TenantSubscriptionStateMachine.CanTransition(
            TenantSubscriptionStatus.PendingPayment,
            TenantSubscriptionStatus.PendingActivation));
        Assert.True(TenantSubscriptionStateMachine.CanTransition(
            TenantSubscriptionStatus.PendingActivation,
            TenantSubscriptionStatus.Active));
    }

    [Fact]
    public void Capacity_wait_and_pending_subscription_are_not_operational_states()
    {
        var capacity = TenantAccessPolicy.Evaluate(new TenantAccessState(
            true,
            TenantStatus.AwaitingDatabaseCapacity,
            TenantSubscriptionStatus.PendingActivation,
            null));
        var pending = TenantAccessPolicy.Evaluate(new TenantAccessState(
            true,
            TenantStatus.PendingSubscription,
            TenantSubscriptionStatus.PendingPayment,
            null));

        Assert.Equal(TenantAccessMode.BillingOnly, capacity.Mode);
        Assert.Equal(TenantAccessMode.BillingOnly, pending.Mode);
    }
}

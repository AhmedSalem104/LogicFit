using LogicFit.Domain.Enums;
using LogicFit.Domain.Services;
using Xunit;

namespace LogicFit.Tests;

public class TenantSubscriptionStateMachineTests
{
    [Fact]
    public void Expired_cannot_transition_to_suspended()
        => Assert.False(TenantSubscriptionStateMachine.CanTransition(TenantSubscriptionStatus.Expired, TenantSubscriptionStatus.Suspended));

    [Fact]
    public void Pending_payment_can_be_activated()
        => Assert.True(TenantSubscriptionStateMachine.CanTransition(TenantSubscriptionStatus.PendingPayment, TenantSubscriptionStatus.Active));

    [Fact]
    public void Cancelled_is_terminal()
        => Assert.False(TenantSubscriptionStateMachine.CanTransition(TenantSubscriptionStatus.Cancelled, TenantSubscriptionStatus.Active));
}

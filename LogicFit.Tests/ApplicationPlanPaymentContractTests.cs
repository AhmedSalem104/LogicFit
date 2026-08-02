using System.Text.Json;
using LogicFit.Application.Common.Services;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Services;
using Xunit;

namespace LogicFit.Tests;

public sealed class ApplicationPlanPaymentContractTests
{
    [Fact]
    public void Plan_snapshot_captures_selected_values_at_submission_time()
    {
        var plan = new Plan
        {
            Name = "Coach Pro",
            Price = 1250m,
            Currency = "EGP",
            MaxMembers = 100,
            BillingCycle = BillingCycle.Monthly
        };

        var snapshot = JsonDocument.Parse(PlanSnapshotFactory.Create(plan, BillingCycle.Annual, DateTime.UtcNow)).RootElement;

        Assert.Equal(plan.Id, snapshot.GetProperty("planId").GetGuid());
        Assert.Equal("Coach Pro", snapshot.GetProperty("planName").GetString());
        Assert.Equal((int)BillingCycle.Annual, snapshot.GetProperty("billingCycle").GetInt32());
        Assert.Equal(1250m, snapshot.GetProperty("finalAmount").GetDecimal());
        Assert.Equal(100, snapshot.GetProperty("limits").GetProperty("maxMembers").GetInt32());
    }

    [Fact]
    public void Payment_review_cannot_start_the_subscription_period()
    {
        Assert.True(TenantSubscriptionStateMachine.CanTransition(
            TenantSubscriptionStatus.PendingPayment,
            TenantSubscriptionStatus.PendingActivation));
        var subscription = new TenantSubscription { Status = TenantSubscriptionStatus.PendingActivation };
        Assert.Null(subscription.StartDate);
        Assert.Null(subscription.EndDate);
    }

    [Fact]
    public void Pending_review_keeps_the_legacy_pending_value_for_existing_rows()
    {
        Assert.Equal(PaymentRequestStatus.Pending, PaymentRequestStatus.PendingReview);
        Assert.Equal(1, (int)PaymentRequestStatus.PendingReview);
    }
}

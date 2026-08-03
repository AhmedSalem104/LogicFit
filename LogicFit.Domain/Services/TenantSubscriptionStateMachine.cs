using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Services;

/// <summary>Allowed platform subscription lifecycle transitions.</summary>
public static class TenantSubscriptionStateMachine
{
    private static readonly IReadOnlyDictionary<TenantSubscriptionStatus, TenantSubscriptionStatus[]> Allowed =
        new Dictionary<TenantSubscriptionStatus, TenantSubscriptionStatus[]>
        {
            [TenantSubscriptionStatus.None] = [TenantSubscriptionStatus.PendingPayment, TenantSubscriptionStatus.Trial, TenantSubscriptionStatus.Active],
            [TenantSubscriptionStatus.PendingPayment] = [TenantSubscriptionStatus.PendingActivation, TenantSubscriptionStatus.Trial, TenantSubscriptionStatus.Active, TenantSubscriptionStatus.Cancelled],
            [TenantSubscriptionStatus.PendingActivation] = [TenantSubscriptionStatus.Trial, TenantSubscriptionStatus.Active, TenantSubscriptionStatus.PendingPayment],
            [TenantSubscriptionStatus.Trial] = [TenantSubscriptionStatus.Active, TenantSubscriptionStatus.PendingPayment, TenantSubscriptionStatus.Expired, TenantSubscriptionStatus.Cancelled],
            [TenantSubscriptionStatus.Active] = [TenantSubscriptionStatus.GracePeriod, TenantSubscriptionStatus.PastDue, TenantSubscriptionStatus.Suspended, TenantSubscriptionStatus.Cancelled, TenantSubscriptionStatus.Expired],
            [TenantSubscriptionStatus.GracePeriod] = [TenantSubscriptionStatus.Active, TenantSubscriptionStatus.Expired, TenantSubscriptionStatus.Cancelled],
            [TenantSubscriptionStatus.PastDue] = [TenantSubscriptionStatus.Active, TenantSubscriptionStatus.GracePeriod, TenantSubscriptionStatus.Cancelled, TenantSubscriptionStatus.Expired],
            [TenantSubscriptionStatus.Suspended] = [TenantSubscriptionStatus.Active, TenantSubscriptionStatus.PendingPayment, TenantSubscriptionStatus.Cancelled, TenantSubscriptionStatus.Expired],
            [TenantSubscriptionStatus.Expired] = [TenantSubscriptionStatus.PendingPayment, TenantSubscriptionStatus.Active],
            [TenantSubscriptionStatus.Cancelled] = [TenantSubscriptionStatus.Expired]
        };

    public static bool CanTransition(TenantSubscriptionStatus from, TenantSubscriptionStatus to)
        => from == to || (Allowed.TryGetValue(from, out var targets) && targets.Contains(to));
}

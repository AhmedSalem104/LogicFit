using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Services;

public static class TenantSubscriptionStateMachine
{
    private static readonly IReadOnlyDictionary<TenantSubscriptionStatus, TenantSubscriptionStatus[]> Allowed =
        new Dictionary<TenantSubscriptionStatus, TenantSubscriptionStatus[]>
        {
            [TenantSubscriptionStatus.PendingPayment] = [TenantSubscriptionStatus.Trial, TenantSubscriptionStatus.Active, TenantSubscriptionStatus.Cancelled],
            [TenantSubscriptionStatus.Trial] = [TenantSubscriptionStatus.Active, TenantSubscriptionStatus.Expired, TenantSubscriptionStatus.Cancelled],
            [TenantSubscriptionStatus.Active] = [TenantSubscriptionStatus.GracePeriod, TenantSubscriptionStatus.PastDue, TenantSubscriptionStatus.Suspended, TenantSubscriptionStatus.Cancelled, TenantSubscriptionStatus.Expired],
            [TenantSubscriptionStatus.GracePeriod] = [TenantSubscriptionStatus.Active, TenantSubscriptionStatus.Expired, TenantSubscriptionStatus.Suspended, TenantSubscriptionStatus.Cancelled],
            [TenantSubscriptionStatus.PastDue] = [TenantSubscriptionStatus.Active, TenantSubscriptionStatus.GracePeriod, TenantSubscriptionStatus.Suspended, TenantSubscriptionStatus.Cancelled, TenantSubscriptionStatus.Expired],
            [TenantSubscriptionStatus.Suspended] = [TenantSubscriptionStatus.Active, TenantSubscriptionStatus.Cancelled, TenantSubscriptionStatus.Expired],
            [TenantSubscriptionStatus.Expired] = [TenantSubscriptionStatus.Active, TenantSubscriptionStatus.Cancelled],
            [TenantSubscriptionStatus.Cancelled] = []
        };

    public static bool CanTransition(TenantSubscriptionStatus from, TenantSubscriptionStatus to)
        => from == to || (Allowed.TryGetValue(from, out var targets) && targets.Contains(to));
}

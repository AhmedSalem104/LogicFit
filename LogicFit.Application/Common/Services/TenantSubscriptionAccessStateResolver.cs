using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Services;

/// <summary>
/// Normalizes a stored subscription lifecycle state into the state governing access right now.
/// Stored cancellation remains immutable history; a cancelled subscription keeps full access until
/// its paid term ends and is then effectively expired.
/// </summary>
public static class TenantSubscriptionAccessStateResolver
{
    public static TenantSubscriptionStatus Resolve(
        TenantSubscriptionStatus status,
        DateTime? endDate,
        DateTime? trialEndsAt,
        DateTime utcNow)
    {
        return status switch
        {
            TenantSubscriptionStatus.Active when endDate.HasValue && endDate.Value <= utcNow
                => TenantSubscriptionStatus.Expired,
            TenantSubscriptionStatus.Trial when trialEndsAt.HasValue && trialEndsAt.Value <= utcNow
                => TenantSubscriptionStatus.Expired,
            TenantSubscriptionStatus.Cancelled when endDate.HasValue && endDate.Value > utcNow
                => TenantSubscriptionStatus.Active,
            TenantSubscriptionStatus.Cancelled when endDate.HasValue && endDate.Value <= utcNow
                => TenantSubscriptionStatus.Expired,
            TenantSubscriptionStatus.PastDue when (endDate ?? trialEndsAt).HasValue &&
                                                   (endDate ?? trialEndsAt)!.Value < utcNow.AddDays(-3)
                => TenantSubscriptionStatus.Expired,
            _ => status
        };
    }
}

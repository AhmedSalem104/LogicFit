using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Services;

/// <summary>The aggregated access-relevant state of a gym: its own status, its latest subscription
/// status, whether it exists, and why it was suspended. Assembled by <c>ITenantAccessGuard</c>.</summary>
public sealed record TenantAccessState(
    bool TenantExists,
    TenantStatus TenantStatus,
    TenantSubscriptionStatus? SubscriptionStatus,
    SuspensionReason? SuspensionReason);

/// <summary>A hard block: the tenant may not be served at all. Carries the error code + HTTP status.</summary>
public sealed record TenantBlock(string Code, int HttpStatus);

/// <summary>
/// Pure, DB-free access rules over a <see cref="TenantAccessState"/>. Decides ONLY allowed-vs-hard-blocked
/// (business nuances like "PendingApproval → billing only" live in the authorization layer, not here).
/// </summary>
public static class TenantAccessPolicy
{
    /// <summary>Returns a <see cref="TenantBlock"/> if the gym must be hard-blocked, otherwise null.</summary>
    public static TenantBlock? EvaluateHardBlock(TenantAccessState s)
    {
        if (!s.TenantExists)
            return new TenantBlock("TENANT_NOT_FOUND", 404);

        // Subscription-level blocks are the most specific — prefer them.
        switch (s.SubscriptionStatus)
        {
            case TenantSubscriptionStatus.Expired:
                return new TenantBlock("TENANT_SUBSCRIPTION_EXPIRED", 402);
            case TenantSubscriptionStatus.Cancelled:
                return new TenantBlock("TENANT_SUBSCRIPTION_CANCELLED", 402);
            case TenantSubscriptionStatus.Suspended:
                return new TenantBlock("TENANT_SUBSCRIPTION_SUSPENDED", 402);
        }

        // Gym-level status.
        return s.TenantStatus switch
        {
            TenantStatus.Suspended => s.SuspensionReason == Domain.Enums.SuspensionReason.NonPayment
                ? new TenantBlock("TENANT_SUSPENDED_NONPAYMENT", 402)
                : new TenantBlock("TENANT_SUSPENDED", 403),
            TenantStatus.Cancelled => new TenantBlock("TENANT_SUBSCRIPTION_CANCELLED", 402),
            TenantStatus.Archived => new TenantBlock("TENANT_ARCHIVED", 403),
            TenantStatus.Deleted => new TenantBlock("TENANT_NOT_FOUND", 404),
            _ => null // Active, Trial, PastDue, PendingApproval → not hard-blocked
        };
    }

    /// <summary>The gym is approved-pending: allowed to sign in but limited to billing/onboarding endpoints.</summary>
    public static bool IsPendingApproval(TenantAccessState s) =>
        s.TenantExists && s.TenantStatus == TenantStatus.PendingApproval;
}

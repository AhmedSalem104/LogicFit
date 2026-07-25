namespace LogicFit.Domain.Enums;

/// <summary>Status of a tenant's subscription to the platform (distinct from a gym member's subscription).</summary>
public enum TenantSubscriptionStatus
{
    PendingPayment = 1,
    Trial = 2,
    Active = 3,
    PastDue = 4,
    Suspended = 5,
    Cancelled = 6,
    Expired = 7,
    GracePeriod = 8
}

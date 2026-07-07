namespace LogicFit.Domain.Enums;

/// <summary>
/// Lifecycle of a gym (tenant) on the platform. Int values intentionally preserve the legacy
/// SubscriptionStatus values that were previously stored in Tenant.Status (Active=1, Suspended=2,
/// Trial=3, Expired→PastDue=4, Cancelled=5) so no data remap is required on upgrade.
/// </summary>
public enum TenantStatus
{
    Active = 1,
    Suspended = 2,
    Trial = 3,
    PastDue = 4,
    Cancelled = 5,
    PendingApproval = 6,
    Archived = 7,
    Deleted = 8
}

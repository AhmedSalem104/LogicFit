namespace LogicFit.Domain.Enums;

/// <summary>Why a gym (tenant) was suspended. Kept separate from Tenant.Status and Subscription.Status
/// so the access layer can produce a precise reason/error code.</summary>
public enum SuspensionReason
{
    None = 0,
    NonPayment = 1,     // subscription lapsed past the grace period (set by the billing lifecycle job)
    ManualByAdmin = 2,  // a platform admin suspended the gym
    Abuse = 3,
    Other = 99
}

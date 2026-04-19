namespace LogicFit.Domain.Enums;

public enum GateAccessResult
{
    Granted = 1,
    Denied = 2
}

public enum GateAccessMethod
{
    Manual = 1,
    Qr = 2,
    Card = 3,
    Face = 4,
    Fingerprint = 5
}

public enum GateDenyReason
{
    None = 0,
    NoActiveSubscription = 1,
    SessionsPerWeekExceeded = 2,
    BranchCapacityFull = 3,
    OutsideOperatingHours = 4,
    SubscriptionFrozen = 5,
    SubscriptionExpired = 6,
    AlreadyCheckedIn = 7,
    CardInactive = 8,
    CardExpired = 9,
    BranchAccessDenied = 10,
    ClientNotFound = 11,
    BranchInactive = 12
}

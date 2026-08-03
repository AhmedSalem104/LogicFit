namespace LogicFit.Domain.Enums;

public enum PaymentRequestStatus
{
    Draft = 0,
    Pending = 1,
    // Alias retained so existing database rows and endpoints remain compatible.
    PendingReview = Pending,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
    Expired = 5
}

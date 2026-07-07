namespace LogicFit.Domain.Enums;

/// <summary>Status of a platform subscription invoice (distinct from a gym's internal InvoiceStatus).</summary>
public enum SubscriptionInvoiceStatus
{
    Unpaid = 1,
    PendingReview = 2,
    Paid = 3,
    Cancelled = 4,
    Overdue = 5
}

namespace LogicFit.Domain.Enums;

public enum StockMovementType
{
    In = 1,          // Purchase, return from client
    Out = 2,         // Sale, damage, loss
    Adjustment = 3,  // Manual correction
    Transfer = 4     // Between branches
}

public enum PurchaseOrderStatus
{
    Draft = 1,
    Submitted = 2,
    Received = 3,
    PartiallyReceived = 4,
    Cancelled = 5
}

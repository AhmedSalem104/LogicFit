namespace LogicFit.Domain.Enums;

public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Overdue = 5,
    Cancelled = 6
}

public enum InvoiceItemType
{
    Subscription = 1,
    Product = 2,
    Class = 3,
    PersonalTraining = 4,
    Manual = 5,
    Other = 99
}

public enum DiscountType
{
    Percentage = 1,
    Fixed = 2
}

public enum CouponApplicability
{
    All = 1,
    Subscriptions = 2,
    Products = 3,
    Classes = 4,
    PersonalTraining = 5
}

namespace LogicFit.Domain.Enums;

public enum SalaryType
{
    Monthly = 1,
    Hourly = 2,
    Daily = 3
}

public enum LeaveType
{
    Annual = 1,
    Sick = 2,
    Unpaid = 3,
    Maternity = 4,
    Emergency = 5,
    Other = 99
}

public enum LeaveStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum CommissionSourceType
{
    SubscriptionSale = 1,
    ProductSale = 2,
    PersonalTraining = 3,
    Manual = 99
}

public enum CommissionStatus
{
    Pending = 1,
    Approved = 2,
    Paid = 3,
    Cancelled = 4
}

public enum CommissionRuleType
{
    Percentage = 1,
    Fixed = 2
}

public enum PayrollStatus
{
    Draft = 1,
    Approved = 2,
    Paid = 3,
    Cancelled = 4
}

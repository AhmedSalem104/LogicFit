namespace LogicFit.Domain.Enums;

public enum ClassEnrollmentStatus
{
    Booked = 1,
    Attended = 2,
    Cancelled = 3,
    NoShow = 4,
    Waitlist = 5
}

public enum RecurrencePattern
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3
}

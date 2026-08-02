namespace LogicFit.Domain.Enums;

public enum RestoreJobStatus
{
    Pending = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    RolledBack = 5,
    Cancelled = 6
}

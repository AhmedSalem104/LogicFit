namespace LogicFit.Domain.Enums;

public enum EquipmentStatus
{
    Active = 1,
    UnderMaintenance = 2,
    OutOfService = 3,
    Retired = 4
}

public enum MaintenanceStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

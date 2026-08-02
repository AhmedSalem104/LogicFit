namespace LogicFit.Domain.Enums;

/// <summary>Lifecycle of a pre-created customer database in the platform resource pool.</summary>
public enum DatabaseResourceStatus
{
    Available = 1,
    Reserved = 2,
    Provisioning = 3,
    Assigned = 4,
    Maintenance = 5,
    RestorePending = 6,
    Faulted = 7,
    Retired = 8
}

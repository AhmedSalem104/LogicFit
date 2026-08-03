namespace LogicFit.Domain.Enums;

public enum ProvisioningJobStatus
{
    Pending = 1,
    AwaitingDatabaseCapacity = 2,
    Provisioning = 3,
    Completed = 4,
    Failed = 5
}

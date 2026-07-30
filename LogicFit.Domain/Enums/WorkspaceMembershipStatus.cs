namespace LogicFit.Domain.Enums;

public enum WorkspaceMembershipStatus
{
    Invited = 1,
    PendingPlatformApproval = 2,
    Active = 3,
    Rejected = 4,
    Suspended = 5,
    Revoked = 6,
    /// <summary>Client has proved their global identity and is awaiting a workspace operator decision.</summary>
    PendingWorkspaceApproval = 7
}

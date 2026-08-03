using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class ApplicationRequest : AuditableEntity
{
    public Guid IdentityAccountId { get; set; }
    public ApplicationType ApplicationType { get; set; }
    public ApplicationRequestStatus Status { get; set; } = ApplicationRequestStatus.Draft;
    public Guid? TargetWorkspaceId { get; set; }
    /// <summary>
    /// The workspace created by an approved workspace-creation request. Kept separate from
    /// TargetWorkspaceId, which identifies the workspace an existing membership request targets.
    /// </summary>
    public Guid? ProvisionedWorkspaceId { get; set; }
    /// <summary>
    /// Non-null duplicate-prevention scope. It is the target workspace ID for membership
    /// requests and a stable workspace-creation scope for workspace applications.
    /// </summary>
    public string TargetScopeKey { get; set; } = string.Empty;
    public string? ReservedWorkspaceIdentifier { get; set; }
    public UserRole? RequestedRole { get; set; }
    public Guid? SponsoredByMembershipId { get; set; }
    public Guid? PlanId { get; set; }
    public BillingCycle? BillingCycle { get; set; }
    /// <summary>Immutable JSON snapshot of the selected plan at submission time.</summary>
    public string? PlanSnapshotJson { get; set; }
    public DateTime? PlanSnapshotAtUtc { get; set; }
    public Guid? PreviousApplicationId { get; set; }
    public int ResubmissionNumber { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string? InformationRequest { get; set; }
    /// <summary>JSON array of payload field names that the applicant may update while more information is requested.</summary>
    public string? RequestedFieldsJson { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public IdentityAccount IdentityAccount { get; set; } = null!;
    public Tenant? TargetWorkspace { get; set; }
    public ICollection<ApplicationRequestRevision> Revisions { get; set; } = new List<ApplicationRequestRevision>();
    public Plan? Plan { get; set; }
}

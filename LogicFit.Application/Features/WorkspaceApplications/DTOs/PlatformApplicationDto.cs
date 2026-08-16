using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.WorkspaceApplications.DTOs;

/// <summary>Review-safe platform view: deliberately excludes health, training, and full payload data.</summary>
public sealed class PlatformApplicationDto
{
    public Guid Id { get; init; }
    public ApplicationType ApplicationType { get; init; }
    public ApplicationRequestStatus Status { get; init; }
    /// <summary>Explicit lifecycle name for clients that consume the unified access contract.</summary>
    public ApplicationRequestStatus ApplicationStatus { get; init; }
    public string ApplicantEmail { get; init; } = string.Empty;
    public string? ApplicantPhoneNumber { get; init; }
    public string? WorkspaceIdentifier { get; init; }
    public UserRole? RequestedRole { get; init; }
    public string? InformationRequest { get; init; }
    public IReadOnlyList<string> RequestedFields { get; init; } = Array.Empty<string>();
    public string? DecisionReason { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? ReviewedBy { get; init; }
    public Guid? ProvisionedWorkspaceId { get; init; }
    public WorkspaceType? WorkspaceType { get; init; }
    public Guid? PaymentRequestId { get; init; }
    public PaymentRequestStatus? PaymentStatus { get; init; }
    public bool HasPaymentProof { get; init; }
    public int PaymentProofVersion { get; init; }
    public TenantStatus? WorkspaceStatus { get; init; }
    public TenantSubscriptionStatus? SubscriptionStatus { get; init; }
    public DatabaseResourceStatus? DatabaseStatus { get; init; }
    /// <summary>Stable operator-facing code that distinguishes an unassigned workspace from an available pool resource.</summary>
    public string? DatabaseStatusCode { get; init; }
    public ProvisioningJobStatus? ProvisioningStatus { get; init; }
    public string UserJourneyStage { get; init; } = "Submitted";
    public bool CanAccessDashboard { get; init; }
    public string? RequiredAction { get; init; }
    public string? NextStep { get; init; }
    public string? UserMessage { get; init; }
    public DateTime? LastUpdatedAtUtc { get; init; }
    public string? ProvisioningErrorCode { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

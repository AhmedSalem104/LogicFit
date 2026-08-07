using System.Text.Json;
using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.WorkspaceApplications.DTOs;

public sealed class FreelanceWorkspaceApplicationPayload
{
    public string WorkspaceName { get; init; } = string.Empty;
    public string WorkspaceIdentifier { get; init; } = string.Empty;
    public string OwnerFullName { get; init; } = string.Empty;
    public string? BrandName { get; init; }
    public string? LogoUrl { get; init; }
    public string? PhotoUrl { get; init; }
    public string? CoverImageUrl { get; init; }
    public string? BackgroundImageUrl { get; init; }
    public string? PrimaryColor { get; init; }
    public string? SecondaryColor { get; init; }
    public string? Bio { get; init; }
    public IReadOnlyList<string> Specialties { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Certifications { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> SocialLinks { get; init; } = new Dictionary<string, string>();
    public string? WelcomeMessage { get; init; }
    public JsonElement? BookingSettings { get; init; }
    public bool MustChangePassword { get; init; }
}

public sealed record ApplicationTrackingSessionDto(
    Guid ApplicationId,
    ApplicationRequestStatus Status,
    string TrackingToken,
    DateTime ExpiresAt);

public sealed class ApplicationTrackingStatusDto
{
    public Guid ApplicationId { get; init; }
    public ApplicationType ApplicationType { get; init; }
    public ApplicationRequestStatus Status { get; init; }
    public string? WorkspaceIdentifier { get; init; }
    public string? InformationRequest { get; init; }
    public IReadOnlyList<string> RequestedFields { get; init; } = Array.Empty<string>();
    public DateTime? SubmittedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public Guid? PlanId { get; init; }
    public BillingCycle? BillingCycle { get; init; }
    public string? PlanSnapshotJson { get; init; }
    public Guid? PaymentRequestId { get; init; }
    public PaymentRequestStatus? PaymentStatus { get; init; }
    public int PaymentProofVersion { get; init; }
    public WorkspaceType? WorkspaceType { get; init; }
    public TenantStatus? WorkspaceStatus { get; init; }
    public TenantSubscriptionStatus? SubscriptionStatus { get; init; }
    public DatabaseResourceStatus? DatabaseStatus { get; init; }
    public string? DatabaseStatusCode { get; init; }
    public ProvisioningJobStatus? ProvisioningStatus { get; init; }
    public bool CanAccessDashboard { get; init; }
    public string? RequiredAction { get; init; }
    public string? NextStep { get; init; }
    public string? UserMessage { get; init; }
    public DateTime? LastUpdatedAtUtc { get; init; }
    public string? ProvisioningErrorCode { get; init; }
    public IReadOnlyDictionary<string, JsonElement> EditableValues { get; init; } = new Dictionary<string, JsonElement>();
}

/// <summary>One-time owner credential handoff returned only by an explicit platform create call.</summary>
public sealed class OneTimeOwnerCredentialsDto
{
    public string Email { get; init; } = string.Empty;
    public string TemporaryPassword { get; init; } = string.Empty;
    public bool MustChangePassword { get; init; } = true;
}

public sealed class PlatformWorkspaceApplicationCreatedDto
{
    public PlatformApplicationDto Application { get; init; } = new();
    public bool NewIdentity { get; init; }
    public OneTimeOwnerCredentialsDto? OneTimeCredentials { get; init; }
}

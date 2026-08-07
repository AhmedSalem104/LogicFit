using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Identity.DTOs;

public sealed class IdentityWorkspaceDto
{
    public Guid WorkspaceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Identifier { get; init; }
    public WorkspaceType WorkspaceType { get; init; }
    public TenantStatus WorkspaceStatus { get; init; }
    public UserRole Role { get; init; }
}

public sealed class PendingApplicationDto
{
    public Guid ApplicationId { get; init; }
    public ApplicationType ApplicationType { get; init; }
    public ApplicationRequestStatus Status { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public string? WorkspaceIdentifier { get; init; }
    public WorkspaceType WorkspaceType { get; init; }
    public PaymentRequestStatus? PaymentStatus { get; init; }
    public TenantStatus? WorkspaceStatus { get; init; }
    public TenantSubscriptionStatus? SubscriptionStatus { get; init; }
    public string? DatabaseStatusCode { get; init; }
    public ProvisioningJobStatus? ProvisioningStatus { get; init; }
    public bool CanAccessDashboard { get; init; }
    public string? RequiredAction { get; init; }
    public string? NextStep { get; init; }
    public string? UserMessage { get; init; }
    public DateTime? LastUpdatedAtUtc { get; init; }
}

public sealed class IdentitySignInDto
{
    public string WorkspaceSelectionToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public IReadOnlyList<IdentityWorkspaceDto> ActiveWorkspaces { get; init; } = Array.Empty<IdentityWorkspaceDto>();
    public IReadOnlyList<PendingApplicationDto> PendingApplications { get; init; } = Array.Empty<PendingApplicationDto>();
    public bool RequiresWorkspaceSelection { get; init; }
}

/// <summary>Minimal invitation data that is safe to show after a recipient follows a high-entropy link.</summary>
public sealed class WorkspaceInvitePreviewDto
{
    public Guid InviteId { get; init; }
    public Guid WorkspaceId { get; init; }
    public string WorkspaceName { get; init; } = string.Empty;
    public string? WorkspaceIdentifier { get; init; }
    public string? LogoUrl { get; init; }
    public UserRole Role { get; init; }
    public string EmailMasked { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

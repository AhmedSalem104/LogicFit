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
}

public sealed class IdentitySignInDto
{
    public string WorkspaceSelectionToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public IReadOnlyList<IdentityWorkspaceDto> ActiveWorkspaces { get; init; } = Array.Empty<IdentityWorkspaceDto>();
    public IReadOnlyList<PendingApplicationDto> PendingApplications { get; init; } = Array.Empty<PendingApplicationDto>();
    public bool RequiresWorkspaceSelection { get; init; }
}

using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.WorkspaceApplications.DTOs;

/// <summary>Review-safe platform view: deliberately excludes health, training, and full payload data.</summary>
public sealed class PlatformApplicationDto
{
    public Guid Id { get; init; }
    public ApplicationType ApplicationType { get; init; }
    public ApplicationRequestStatus Status { get; init; }
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
    public string RowVersion { get; init; } = string.Empty;
}

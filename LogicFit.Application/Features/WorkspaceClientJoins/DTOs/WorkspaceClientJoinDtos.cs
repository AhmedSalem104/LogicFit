using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.WorkspaceClientJoins.DTOs;

public sealed class WorkspaceClientJoinCodeDto
{
    public string Code { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public bool AutoApproveClients { get; init; }
}

public sealed class WorkspaceClientJoinPreviewDto
{
    public Guid WorkspaceId { get; init; }
    public string WorkspaceName { get; init; } = string.Empty;
    public string? WorkspaceIdentifier { get; init; }
    public string? LogoUrl { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool RequiresWorkspaceApproval { get; init; }
}

public sealed class ClientJoinResultDto
{
    public Guid WorkspaceId { get; init; }
    public WorkspaceMembershipStatus MembershipStatus { get; init; }
}

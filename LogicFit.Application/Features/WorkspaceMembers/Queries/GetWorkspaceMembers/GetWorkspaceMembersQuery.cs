using LogicFit.Application.Features.WorkspaceMembers.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceMembers.Queries.GetWorkspaceMembers;

public sealed class GetWorkspaceMembersQuery : IRequest<IReadOnlyList<WorkspaceMemberDto>>
{
    public UserRole? Role { get; init; }
    public string? AccessStatus { get; init; }
    public string? SearchTerm { get; init; }
}

using LogicFit.Application.Features.WorkspaceMembers.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceMembers.Commands.ChangeWorkspaceMemberStatus;

public enum WorkspaceMemberStatusAction
{
    Suspend = 1,
    Activate = 2,
    Remove = 3
}

public sealed record ChangeWorkspaceMemberStatusCommand(Guid MembershipId, WorkspaceMemberStatusAction Action) : IRequest<WorkspaceMemberDto>;

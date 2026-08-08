using LogicFit.Application.Features.WorkspaceMembers.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceMembers.Commands.ResetWorkspaceMemberPassword;

public sealed record ResetWorkspaceMemberPasswordCommand(Guid MembershipId) : IRequest<WorkspaceMemberCreatedDto>;

using MediatR;

namespace LogicFit.Application.Features.WorkspaceClientJoins.Commands.ApproveWorkspaceClientMembership;

public sealed record ApproveWorkspaceClientMembershipCommand(Guid MembershipId) : IRequest;

using LogicFit.Application.Features.WorkspaceMembers.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceMembers.Commands.CreateWorkspaceMember;

public sealed class CreateWorkspaceMemberCommand : IRequest<WorkspaceMemberCreatedDto>
{
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string FullName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
}

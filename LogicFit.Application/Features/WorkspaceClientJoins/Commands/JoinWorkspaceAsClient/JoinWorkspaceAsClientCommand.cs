using FluentValidation;
using LogicFit.Application.Features.WorkspaceClientJoins.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceClientJoins.Commands.JoinWorkspaceAsClient;

public sealed record JoinWorkspaceAsClientCommand(string Code, string WorkspaceSelectionToken) : IRequest<ClientJoinResultDto>;

public sealed class JoinWorkspaceAsClientValidator : AbstractValidator<JoinWorkspaceAsClientCommand>
{
    public JoinWorkspaceAsClientValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(512);
        RuleFor(x => x.WorkspaceSelectionToken).NotEmpty().MaximumLength(512);
    }
}

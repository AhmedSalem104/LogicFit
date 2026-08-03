using FluentValidation;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceInvites.Commands.AcceptWorkspaceInvite;

public sealed record AcceptWorkspaceInviteCommand(
    string Token,
    string WorkspaceSelectionToken) : IRequest;

public sealed class AcceptWorkspaceInviteValidator : AbstractValidator<AcceptWorkspaceInviteCommand>
{
    public AcceptWorkspaceInviteValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
        RuleFor(x => x.WorkspaceSelectionToken).NotEmpty().MaximumLength(512);
    }
}

using FluentValidation;
using LogicFit.Application.Features.Auth.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Identity.Commands.SelectIdentityWorkspace;

public sealed record SelectIdentityWorkspaceCommand(string WorkspaceSelectionToken, Guid WorkspaceId) : IRequest<AuthResponseDto>;

public sealed class SelectIdentityWorkspaceValidator : AbstractValidator<SelectIdentityWorkspaceCommand>
{
    public SelectIdentityWorkspaceValidator()
    {
        RuleFor(x => x.WorkspaceSelectionToken).NotEmpty().MaximumLength(128);
        RuleFor(x => x.WorkspaceId).NotEmpty();
    }
}

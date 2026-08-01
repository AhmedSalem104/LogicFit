using FluentValidation;
using LogicFit.Application.Features.Identity.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceInvites.Commands.PreviewWorkspaceInvite;

public sealed record PreviewWorkspaceInviteCommand(string Token) : IRequest<WorkspaceInvitePreviewDto>;

public sealed class PreviewWorkspaceInviteValidator : AbstractValidator<PreviewWorkspaceInviteCommand>
{
    public PreviewWorkspaceInviteValidator() => RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
}

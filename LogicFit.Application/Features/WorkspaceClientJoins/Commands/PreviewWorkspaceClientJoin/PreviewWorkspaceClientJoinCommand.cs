using FluentValidation;
using LogicFit.Application.Features.WorkspaceClientJoins.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceClientJoins.Commands.PreviewWorkspaceClientJoin;

public sealed record PreviewWorkspaceClientJoinCommand(string Code) : IRequest<WorkspaceClientJoinPreviewDto>;

public sealed class PreviewWorkspaceClientJoinValidator : AbstractValidator<PreviewWorkspaceClientJoinCommand>
{
    public PreviewWorkspaceClientJoinValidator() => RuleFor(x => x.Code).NotEmpty().MaximumLength(512);
}

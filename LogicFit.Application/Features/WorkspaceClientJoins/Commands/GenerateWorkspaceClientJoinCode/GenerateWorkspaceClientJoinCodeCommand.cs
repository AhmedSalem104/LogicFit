using FluentValidation;
using LogicFit.Application.Features.WorkspaceClientJoins.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceClientJoins.Commands.GenerateWorkspaceClientJoinCode;

public sealed record GenerateWorkspaceClientJoinCodeCommand(bool AutoApproveClients, int ValidForDays = 30) : IRequest<WorkspaceClientJoinCodeDto>;

public sealed class GenerateWorkspaceClientJoinCodeValidator : AbstractValidator<GenerateWorkspaceClientJoinCodeCommand>
{
    public GenerateWorkspaceClientJoinCodeValidator()
    {
        RuleFor(x => x.ValidForDays).InclusiveBetween(1, 90);
    }
}

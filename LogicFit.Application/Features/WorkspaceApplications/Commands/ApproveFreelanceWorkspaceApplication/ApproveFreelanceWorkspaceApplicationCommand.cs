using FluentValidation;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.ApproveFreelanceWorkspaceApplication;

public sealed record ApproveFreelanceWorkspaceApplicationCommand(Guid ApplicationId, string RowVersion) : IRequest<PlatformApplicationDto>;

public sealed class ApproveFreelanceWorkspaceApplicationValidator : AbstractValidator<ApproveFreelanceWorkspaceApplicationCommand>
{
    public ApproveFreelanceWorkspaceApplicationValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}

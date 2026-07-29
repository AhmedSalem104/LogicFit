using FluentValidation;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.ApproveMembershipApplication;

public sealed record ApproveMembershipApplicationCommand(Guid ApplicationId, string RowVersion) : IRequest<PlatformApplicationDto>;

public sealed class ApproveMembershipApplicationValidator : AbstractValidator<ApproveMembershipApplicationCommand>
{
    public ApproveMembershipApplicationValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}

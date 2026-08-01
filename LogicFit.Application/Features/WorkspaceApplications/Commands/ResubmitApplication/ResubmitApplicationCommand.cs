using FluentValidation;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.ResubmitApplication;

public sealed record ResubmitApplicationCommand(string TrackingToken) : IRequest<ApplicationTrackingStatusDto>;

public sealed class ResubmitApplicationValidator : AbstractValidator<ResubmitApplicationCommand>
{
    public ResubmitApplicationValidator() => RuleFor(x => x.TrackingToken).NotEmpty().MaximumLength(128);
}

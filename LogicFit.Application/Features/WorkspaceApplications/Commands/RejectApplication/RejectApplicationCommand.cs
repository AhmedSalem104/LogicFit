using FluentValidation;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.RejectApplication;

public sealed class RejectApplicationCommand : IRequest<PlatformApplicationDto>
{
    public Guid ApplicationId { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

public sealed class RejectApplicationValidator : AbstractValidator<RejectApplicationCommand>
{
    public RejectApplicationValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
    }
}

using FluentValidation;
using LogicFit.Application.Features.Identity.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceInvites.Commands.AcceptWorkspaceInvite;

public sealed record RequestWorkspaceInviteOtpCommand(
    string Token,
    string WorkspaceSelectionToken,
    string? SessionBinding) : IRequest<OtpChallengeDto>;

public sealed record AcceptWorkspaceInviteCommand(
    string Token,
    string WorkspaceSelectionToken,
    Guid? ChallengeId = null,
    string? Code = null,
    string? SessionBinding = null) : IRequest;

public sealed class AcceptWorkspaceInviteValidator : AbstractValidator<AcceptWorkspaceInviteCommand>
{
    public AcceptWorkspaceInviteValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
        RuleFor(x => x.WorkspaceSelectionToken).NotEmpty().MaximumLength(512);
        When(x => x.ChallengeId.HasValue || !string.IsNullOrWhiteSpace(x.Code), () =>
        {
            RuleFor(x => x.ChallengeId).NotNull();
            RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
        });
    }
}

public sealed class RequestWorkspaceInviteOtpValidator : AbstractValidator<RequestWorkspaceInviteOtpCommand>
{
    public RequestWorkspaceInviteOtpValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
        RuleFor(x => x.WorkspaceSelectionToken).NotEmpty().MaximumLength(512);
    }
}

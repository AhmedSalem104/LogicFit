using FluentValidation;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.StartApplicationReview;

public sealed record StartApplicationReviewCommand(Guid ApplicationId, string RowVersion) : IRequest<PlatformApplicationDto>;

public sealed class StartApplicationReviewValidator : AbstractValidator<StartApplicationReviewCommand>
{
    public StartApplicationReviewValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty().Must(IsBase64).WithMessage("RowVersion must be Base64.");
    }

    private static bool IsBase64(string value)
    {
        try { Convert.FromBase64String(value); return true; }
        catch (FormatException) { return false; }
    }
}

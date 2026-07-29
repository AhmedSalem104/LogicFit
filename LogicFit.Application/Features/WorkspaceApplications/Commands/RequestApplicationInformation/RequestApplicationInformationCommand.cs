using FluentValidation;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.RequestApplicationInformation;

public sealed class RequestApplicationInformationCommand : IRequest<PlatformApplicationDto>
{
    public Guid ApplicationId { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> RequestedFields { get; init; } = Array.Empty<string>();
}

public sealed class RequestApplicationInformationValidator : AbstractValidator<RequestApplicationInformationCommand>
{
    public RequestApplicationInformationValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.RequestedFields).NotEmpty().Must(x => x.Count <= 20);
        RuleForEach(x => x.RequestedFields).NotEmpty().MaximumLength(100);
    }
}

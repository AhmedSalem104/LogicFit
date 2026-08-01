using System.Text.Json;
using FluentValidation;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.UpdateApplicationRequestedFields;

public sealed class UpdateApplicationRequestedFieldsCommand : IRequest<ApplicationTrackingStatusDto>
{
    public string TrackingToken { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, JsonElement> Fields { get; init; } = new Dictionary<string, JsonElement>();
}

public sealed class UpdateApplicationRequestedFieldsValidator : AbstractValidator<UpdateApplicationRequestedFieldsCommand>
{
    public UpdateApplicationRequestedFieldsValidator()
    {
        RuleFor(x => x.TrackingToken).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Fields).NotEmpty();
        RuleForEach(x => x.Fields).ChildRules(pair => pair.RuleFor(x => x.Key).NotEmpty().MaximumLength(100));
    }
}

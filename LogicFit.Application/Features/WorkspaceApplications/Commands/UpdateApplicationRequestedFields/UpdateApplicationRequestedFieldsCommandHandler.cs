using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Application.Features.WorkspaceApplications.Queries.GetApplicationTrackingStatus;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.UpdateApplicationRequestedFields;

public sealed class UpdateApplicationRequestedFieldsCommandHandler
    : IRequestHandler<UpdateApplicationRequestedFieldsCommand, ApplicationTrackingStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public UpdateApplicationRequestedFieldsCommandHandler(IApplicationDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<ApplicationTrackingStatusDto> Handle(
        UpdateApplicationRequestedFieldsCommand request,
        CancellationToken cancellationToken)
    {
        var session = await ApplicationTrackingSessionResolver.GetActiveAsync(
            _context, _dateTimeService, request.TrackingToken, cancellationToken);
        var application = session.ApplicationRequest;
        if (application.Status != ApplicationRequestStatus.NeedsMoreInformation)
            throw new ConflictException("Only applications awaiting more information can be edited.");

        var requestedFields = GetApplicationTrackingStatusQueryHandler.ReadStringList(application.RequestedFieldsJson);
        if (request.Fields.Keys.Any(key => key.Equals("PaymentProof", StringComparison.Ordinal)))
            throw new ValidationException("PaymentProof", "Upload the payment proof through the payment-proof upload action.");
        if (request.Fields.Keys.Any(key => !requestedFields.Contains(key, StringComparer.Ordinal)))
            throw new ForbiddenException("Only fields explicitly requested by the platform can be edited.");

        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(application.PayloadJson)
            ?? new Dictionary<string, JsonElement>();
        foreach (var (key, value) in request.Fields)
            payload[key] = value.Clone();

        application.PayloadJson = JsonSerializer.Serialize(payload);
        await _context.SaveChangesAsync(cancellationToken);

        return new ApplicationTrackingStatusDto
        {
            ApplicationId = application.Id,
            ApplicationType = application.ApplicationType,
            Status = application.Status,
            WorkspaceIdentifier = application.ReservedWorkspaceIdentifier,
            InformationRequest = application.InformationRequest,
            RequestedFields = requestedFields,
            SubmittedAt = application.SubmittedAt,
            ReviewedAt = application.ReviewedAt,
            EditableValues = request.Fields.ToDictionary(x => x.Key, x => x.Value.Clone(), StringComparer.Ordinal)
        };
    }
}

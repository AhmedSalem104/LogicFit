using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceApplications.Queries.GetApplicationTrackingStatus;

public sealed class GetApplicationTrackingStatusQueryHandler
    : IRequestHandler<GetApplicationTrackingStatusQuery, ApplicationTrackingStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public GetApplicationTrackingStatusQueryHandler(IApplicationDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<ApplicationTrackingStatusDto> Handle(GetApplicationTrackingStatusQuery request, CancellationToken cancellationToken)
    {
        var session = await ApplicationTrackingSessionResolver.GetActiveAsync(
            _context, _dateTimeService, request.TrackingToken, cancellationToken);
        var application = session.ApplicationRequest;
        var requestedFields = ReadStringList(application.RequestedFieldsJson);
        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(application.PayloadJson)
            ?? new Dictionary<string, JsonElement>();
        var editable = payload
            .Where(x => requestedFields.Contains(x.Key, StringComparer.Ordinal))
            .ToDictionary(x => x.Key, x => x.Value.Clone(), StringComparer.Ordinal);

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
            EditableValues = editable
        };
    }

    internal static IReadOnlyList<string> ReadStringList(string? json) => string.IsNullOrWhiteSpace(json)
        ? Array.Empty<string>()
        : JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
}

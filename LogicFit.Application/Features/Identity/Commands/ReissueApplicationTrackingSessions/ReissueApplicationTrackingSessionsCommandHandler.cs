using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkspaceApplications;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Identity.Commands.ReissueApplicationTrackingSessions;

public sealed class ReissueApplicationTrackingSessionsCommandHandler
    : IRequestHandler<ReissueApplicationTrackingSessionsCommand, IReadOnlyList<ApplicationTrackingSessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICurrentUserService _currentUserService;

    public ReissueApplicationTrackingSessionsCommandHandler(
        IApplicationDbContext context,
        IDateTimeService dateTimeService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ApplicationTrackingSessionDto>> Handle(
        ReissueApplicationTrackingSessionsCommand request,
        CancellationToken cancellationToken)
    {
        var identitySession = await IdentityWorkspaceSessionResolver.GetActiveAsync(
            _context, _dateTimeService, request.WorkspaceSelectionToken, cancellationToken);
        var applications = await _context.ApplicationRequests
            .Where(x => x.IdentityAccountId == identitySession.IdentityAccountId &&
                        (x.Status == ApplicationRequestStatus.Draft ||
                         x.Status == ApplicationRequestStatus.Submitted ||
                         x.Status == ApplicationRequestStatus.UnderReview ||
                         x.Status == ApplicationRequestStatus.NeedsMoreInformation))
            .ToListAsync(cancellationToken);
        var now = _dateTimeService.UtcNow;
        var results = new List<ApplicationTrackingSessionDto>();
        foreach (var application in applications)
        {
            var token = ApplicationTrackingToken.CreateRaw();
            var session = new ApplicationTrackingSession
            {
                ApplicationRequestId = application.Id,
                TokenHash = ApplicationTrackingToken.Hash(token),
                ExpiresAt = now.AddMinutes(30),
                CreatedByIp = _currentUserService.IpAddress
            };
            _context.ApplicationTrackingSessions.Add(session);
            results.Add(new ApplicationTrackingSessionDto(application.Id, application.Status, token, session.ExpiresAt));
        }
        await _context.SaveChangesAsync(cancellationToken);
        return results;
    }
}

using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Application.Features.WorkspaceApplications.Queries.GetApplicationTrackingStatus;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.ResubmitApplication;

public sealed class ResubmitApplicationCommandHandler
    : IRequestHandler<ResubmitApplicationCommand, ApplicationTrackingStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public ResubmitApplicationCommandHandler(IApplicationDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<ApplicationTrackingStatusDto> Handle(ResubmitApplicationCommand request, CancellationToken cancellationToken)
    {
        var session = await ApplicationTrackingSessionResolver.GetActiveAsync(
            _context, _dateTimeService, request.TrackingToken, cancellationToken);
        var application = session.ApplicationRequest;
        if (!ApplicationRequestStateMachine.CanTransition(application.Status, ApplicationRequestStatus.Submitted))
            throw new ConflictException("This application cannot be resubmitted.");

        if (application.ApplicationType is ApplicationType.GymWorkspaceCreation or ApplicationType.FreelanceWorkspaceCreation)
        {
            var payment = await _context.PaymentRequests
                .Include(x => x.Proofs)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ApplicationRequestId == application.Id && !x.IsDeleted, cancellationToken);
            var hasProof = payment is not null &&
                (!string.IsNullOrWhiteSpace(payment.ProofFileUrl) || payment.Proofs.Any(x => x.IsCurrent));
            if (payment is not null &&
                (payment.Status == PaymentRequestStatus.Draft || payment.Status == PaymentRequestStatus.Pending) &&
                !hasProof)
                throw new ConflictException("PAYMENT_PROOF_REQUIRED", "Upload a payment proof before resubmitting this workspace application.");
        }

        var nextRevision = await _context.ApplicationRequestRevisions
            .Where(x => x.ApplicationRequestId == application.Id)
            .Select(x => (int?)x.RevisionNumber)
            .MaxAsync(cancellationToken) ?? 0;
        var now = _dateTimeService.UtcNow;
        application.Status = ApplicationRequestStatus.Submitted;
        application.SubmittedAt = now;
        application.InformationRequest = null;
        application.RequestedFieldsJson = null;
        _context.ApplicationRequestRevisions.Add(new ApplicationRequestRevision
        {
            ApplicationRequestId = application.Id,
            RevisionNumber = nextRevision + 1,
            PayloadJson = application.PayloadJson,
            SubmittedAt = now,
            SubmittedBy = application.IdentityAccountId.ToString()
        });
        await _context.SaveChangesAsync(cancellationToken);

        return new ApplicationTrackingStatusDto
        {
            ApplicationId = application.Id,
            ApplicationType = application.ApplicationType,
            Status = application.Status,
            WorkspaceIdentifier = application.ReservedWorkspaceIdentifier,
            RequestedFields = Array.Empty<string>(),
            SubmittedAt = application.SubmittedAt,
            ReviewedAt = application.ReviewedAt,
            EditableValues = new Dictionary<string, JsonElement>()
        };
    }
}

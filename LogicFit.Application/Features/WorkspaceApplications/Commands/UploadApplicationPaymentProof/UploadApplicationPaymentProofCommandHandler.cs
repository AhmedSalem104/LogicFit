using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.UploadApplicationPaymentProof;

public sealed class UploadApplicationPaymentProofCommandHandler
    : IRequestHandler<UploadApplicationPaymentProofCommand, ApplicationPaymentProofUploadedDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _clock;
    private readonly ICurrentUserService _currentUser;

    public UploadApplicationPaymentProofCommandHandler(
        IApplicationDbContext context,
        IDateTimeService clock,
        ICurrentUserService currentUser)
        => (_context, _clock, _currentUser) = (context, clock, currentUser);

    public async Task<ApplicationPaymentProofUploadedDto> Handle(
        UploadApplicationPaymentProofCommand request,
        CancellationToken cancellationToken)
    {
        var session = await ApplicationTrackingSessionResolver.GetActiveAsync(
            _context, _clock, request.TrackingToken, cancellationToken);
        var application = session.ApplicationRequest;

        if (application.ApplicationType is not (ApplicationType.GymWorkspaceCreation or ApplicationType.FreelanceWorkspaceCreation))
            throw new ForbiddenException("Payment proof upload is only available for workspace applications.");
        if (application.Status != ApplicationRequestStatus.NeedsMoreInformation)
            throw new ConflictException("PAYMENT_PROOF_UPLOAD_NOT_AVAILABLE", "The application must be awaiting additional information before a proof can be uploaded.");

        var payment = await _context.PaymentRequests
            .Include(x => x.Proofs)
            .FirstOrDefaultAsync(x => x.ApplicationRequestId == application.Id && !x.IsDeleted, cancellationToken)
            ?? throw new ConflictException("PAYMENT_REQUEST_MISSING", "The application has no editable payment request.");

        if (payment.Status is not (PaymentRequestStatus.Draft or PaymentRequestStatus.Pending))
            throw new ConflictException("PAYMENT_PROOF_NOT_EDITABLE", "A proof can only be attached before payment review is completed.");

        Validate(request);

        var nextVersion = payment.Proofs.Count == 0 ? 1 : payment.Proofs.Max(x => x.Version) + 1;
        foreach (var proof in payment.Proofs.Where(x => x.IsCurrent))
            proof.IsCurrent = false;

        var now = _clock.UtcNow;
        _context.PaymentProofs.Add(new PaymentProof
        {
            PaymentRequestId = payment.Id,
            Version = nextVersion,
            StorageKey = request.ProofStorageKey,
            OriginalFileName = request.OriginalFileName.Trim(),
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            Sha256 = request.Sha256.ToUpperInvariant(),
            IsCurrent = true,
            UploadedBy = application.IdentityAccountId.ToString(),
            UploadedAtUtc = now
        });
        payment.ProofFileUrl = request.ProofStorageKey;
        payment.RejectReason = null;

        SecurityAuditLog.Add(
            _context,
            _currentUser,
            _clock,
            "WorkspaceApplicationPaymentProofUploaded",
            true,
            application.Id,
            payment.TenantId);

        await _context.SaveChangesAsync(cancellationToken);

        return new ApplicationPaymentProofUploadedDto
        {
            ApplicationId = application.Id,
            Version = nextVersion,
            OriginalFileName = request.OriginalFileName.Trim(),
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes
        };
    }

    private static void Validate(UploadApplicationPaymentProofCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.ProofStorageKey))
            throw new ValidationException("PaymentProof", "A payment proof file is required.");
        if (request.SizeBytes <= 0 || request.SizeBytes > 10 * 1024 * 1024)
            throw new ValidationException("PaymentProof", "The payment proof must be between 1 byte and 10 MB.");
        if (request.ContentType is not ("image/jpeg" or "image/png" or "application/pdf"))
            throw new ValidationException("PaymentProof", "Payment proof must be a JPEG, PNG, or PDF.");
        if (string.IsNullOrWhiteSpace(request.OriginalFileName) || request.OriginalFileName.Length > 255)
            throw new ValidationException("PaymentProof", "The payment proof file name is invalid.");
        if (request.Sha256.Length != 64 || request.Sha256.Any(value => !Uri.IsHexDigit(value)))
            throw new ValidationException("PaymentProof", "The payment proof checksum is invalid.");
    }
}

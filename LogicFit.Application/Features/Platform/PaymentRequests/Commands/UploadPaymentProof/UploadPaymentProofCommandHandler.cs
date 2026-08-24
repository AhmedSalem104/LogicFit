using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.PaymentRequests.Commands.UploadPaymentProof;

public sealed class UploadPaymentProofCommandHandler
    : IRequestHandler<UploadPaymentProofCommand, PaymentRequestDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _clock;

    public UploadPaymentProofCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService clock)
        => (_context, _currentUser, _clock) = (context, currentUser, clock);

    public async Task<PaymentRequestDto> Handle(
        UploadPaymentProofCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await _context.PaymentRequests
            .Include(x => x.Plan)
            .Include(x => x.Tenant)
            .Include(x => x.Proofs)
            .FirstOrDefaultAsync(x => x.Id == request.PaymentRequestId && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentRequest), request.PaymentRequestId);

        if (payment.Status is not (PaymentRequestStatus.Draft or PaymentRequestStatus.Pending))
            throw new ConflictException("PAYMENT_PROOF_NOT_EDITABLE", "A proof can only be attached before payment review is completed.");

        if (string.IsNullOrWhiteSpace(request.ProofFileUrl))
            throw new ValidationException("PaymentProof", "A payment proof file is required.");
        if (request.SizeBytes <= 0 || request.SizeBytes > 10 * 1024 * 1024)
            throw new ValidationException("PaymentProof", "The payment proof must be between 1 byte and 10 MB.");
        if (request.ContentType is not ("image/jpeg" or "image/png" or "application/pdf"))
            throw new ValidationException("PaymentProof", "Payment proof must be a JPEG, PNG, or PDF.");
        if (string.IsNullOrWhiteSpace(request.OriginalFileName) || request.OriginalFileName.Length > 255)
            throw new ValidationException("PaymentProof", "The payment proof file name is invalid.");
        if (request.Sha256.Length != 64 || request.Sha256.Any(value => !Uri.IsHexDigit(value)))
            throw new ValidationException("PaymentProof", "The payment proof checksum is invalid.");

        var nextVersion = payment.Proofs.Count == 0 ? 1 : payment.Proofs.Max(x => x.Version) + 1;
        foreach (var proof in payment.Proofs.Where(x => x.IsCurrent))
            proof.IsCurrent = false;

        var now = _clock.UtcNow;
        _context.PaymentProofs.Add(new PaymentProof
        {
            PaymentRequestId = payment.Id,
            Version = nextVersion,
            StorageKey = request.ProofFileUrl,
            OriginalFileName = request.OriginalFileName.Trim(),
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            Sha256 = request.Sha256.ToUpperInvariant(),
            IsCurrent = true,
            UploadedBy = _currentUser.UserId,
            UploadedAtUtc = now
        });
        payment.ProofFileUrl = request.ProofFileUrl;
        payment.RejectReason = null;
        SecurityAuditLog.Add(_context, _currentUser, _clock, "PlatformPaymentProofUploaded", true, payment.Id, payment.TenantId);
        await _context.SaveChangesAsync(cancellationToken);

        return new PaymentRequestDto
        {
            Id = payment.Id,
            TenantId = payment.TenantId,
            TenantName = payment.Tenant?.Name,
            PlanId = payment.PlanId,
            PlanName = payment.Plan?.Name,
            TenantSubscriptionId = payment.TenantSubscriptionId,
            ApplicationRequestId = payment.ApplicationRequestId,
            IdentityAccountId = payment.IdentityAccountId,
            BillingCycle = payment.BillingCycle,
            PlanSnapshotJson = payment.PlanSnapshotJson,
            ProofVersion = nextVersion,
            Operation = payment.Operation,
            Amount = payment.Amount,
            Currency = payment.Currency,
            PaymentMethodId = payment.PaymentMethodId,
            TransactionNumber = payment.TransactionNumber,
            PaymentDate = payment.PaymentDate,
            ProofFileUrl = payment.ProofFileUrl,
            Notes = payment.Notes,
            Status = payment.Status,
            ReviewedBy = payment.ReviewedBy,
            ReviewedAt = payment.ReviewedAt,
            RejectReason = payment.RejectReason,
            CreatedAt = payment.CreatedAt
        };
    }
}

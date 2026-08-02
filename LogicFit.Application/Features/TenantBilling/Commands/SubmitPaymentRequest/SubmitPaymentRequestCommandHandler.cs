using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.TenantBilling.Commands.SubmitPaymentRequest;

public class SubmitPaymentRequestCommandHandler : IRequestHandler<SubmitPaymentRequestCommand, PaymentRequestDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public SubmitPaymentRequestCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<PaymentRequestDto> Handle(SubmitPaymentRequestCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var plan = await _context.Plans
            .Include(p => p.PlanFeatures)
            .ThenInclude(p => p.Feature)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive, cancellationToken);
        if (plan == null)
        {
            throw new NotFoundException(nameof(Plan), request.PlanId);
        }

        // Reuse an existing pending subscription for this plan, or open a new one.
        var subscription = await _context.TenantSubscriptions
            .Where(s => s.TenantId == tenantId && s.PlanId == request.PlanId &&
                        s.Status == TenantSubscriptionStatus.PendingPayment)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription == null)
        {
            subscription = new TenantSubscription
            {
                TenantId = tenantId,
                PlanId = plan.Id,
                Status = TenantSubscriptionStatus.PendingPayment,
                BillingCycle = plan.BillingCycle,
                Amount = plan.Price,
                Currency = plan.Currency
            };
            _context.TenantSubscriptions.Add(subscription);
        }

        var paymentRequest = new PaymentRequest
        {
            TenantId = tenantId,
            TenantSubscriptionId = subscription.Id,
            PlanId = plan.Id,
            BillingCycle = request.BillingCycle ?? plan.BillingCycle,
            PlanSnapshotJson = PlanSnapshotFactory.Create(plan, request.BillingCycle ?? plan.BillingCycle, _dateTimeService.UtcNow),
            IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim(),
            Amount = plan.Price,
            Currency = plan.Currency,
            PaymentMethodId = request.PaymentMethodId,
            TransactionNumber = request.TransactionNumber,
            PaymentDate = request.PaymentDate ?? _dateTimeService.UtcNow,
            ProofFileUrl = request.ProofFileUrl,
            Notes = request.Notes,
            Operation = request.Operation,
            ExtensionDays = request.ExtensionDays,
            Status = PaymentRequestStatus.PendingReview
        };
        _context.PaymentRequests.Add(paymentRequest);

        if (!string.IsNullOrWhiteSpace(request.ProofStorageKey))
        {
            if (request.ProofContentType is not ("image/jpeg" or "image/png" or "application/pdf") ||
                request.ProofSizeBytes is not > 0 and <= 10 * 1024 * 1024 ||
                string.IsNullOrWhiteSpace(request.ProofSha256) || request.ProofSha256.Length != 64)
                throw new ValidationException("PaymentProof", "The private payment proof metadata is invalid.");

            _context.PaymentProofs.Add(new PaymentProof
            {
                PaymentRequestId = paymentRequest.Id,
                Version = 1,
                StorageKey = request.ProofStorageKey.Trim(),
                OriginalFileName = request.ProofOriginalFileName?.Trim() ?? "proof",
                ContentType = request.ProofContentType,
                SizeBytes = request.ProofSizeBytes.Value,
                Sha256 = request.ProofSha256.ToUpperInvariant(),
                UploadedAtUtc = _dateTimeService.UtcNow,
                UploadedBy = null
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new PaymentRequestDto
        {
            Id = paymentRequest.Id,
            TenantId = paymentRequest.TenantId,
            PlanId = paymentRequest.PlanId,
            PlanName = plan.Name,
            TenantSubscriptionId = paymentRequest.TenantSubscriptionId,
            BillingCycle = paymentRequest.BillingCycle,
            PlanSnapshotJson = paymentRequest.PlanSnapshotJson,
            Operation = paymentRequest.Operation,
            Amount = paymentRequest.Amount,
            Currency = paymentRequest.Currency,
            PaymentMethodId = paymentRequest.PaymentMethodId,
            TransactionNumber = paymentRequest.TransactionNumber,
            PaymentDate = paymentRequest.PaymentDate,
            ProofFileUrl = paymentRequest.ProofFileUrl,
            Notes = paymentRequest.Notes,
            Status = paymentRequest.Status,
            CreatedAt = paymentRequest.CreatedAt
        };
    }
}

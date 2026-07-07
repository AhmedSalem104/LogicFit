using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.PaymentRequests.Commands.RejectPaymentRequest;

public class RejectPaymentRequestCommandHandler : IRequestHandler<RejectPaymentRequestCommand, PaymentRequestDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly INotificationService _notificationService;

    public RejectPaymentRequestCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        INotificationService notificationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _notificationService = notificationService;
    }

    public async Task<PaymentRequestDto> Handle(RejectPaymentRequestCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RejectReason))
        {
            throw new ValidationException("RejectReason", "A reason is required to reject a payment.");
        }

        var pr = await _context.PaymentRequests
            .Include(p => p.Plan)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentRequestId, cancellationToken);

        if (pr == null)
        {
            throw new NotFoundException(nameof(PaymentRequest), request.PaymentRequestId);
        }

        if (pr.Status != PaymentRequestStatus.Pending)
        {
            // Idempotent: only a pending request can be rejected.
            throw new ConflictException($"Payment request is already {pr.Status}");
        }

        pr.Status = PaymentRequestStatus.Rejected;
        pr.RejectReason = request.RejectReason;
        pr.ReviewedBy = _currentUserService.UserId;
        pr.ReviewedAt = _dateTimeService.UtcNow;

        // The subscription stays PendingPayment so the owner can resubmit proof.
        await _notificationService.NotifyTenantOwnerAsync(
            pr.TenantId,
            Common.Notifications.NotificationTemplates.PaymentRequestRejected,
            new Dictionary<string, string> { ["reason"] = request.RejectReason },
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new PaymentRequestDto
        {
            Id = pr.Id,
            TenantId = pr.TenantId,
            PlanId = pr.PlanId,
            PlanName = pr.Plan?.Name,
            TenantSubscriptionId = pr.TenantSubscriptionId,
            Amount = pr.Amount,
            Currency = pr.Currency,
            PaymentMethodId = pr.PaymentMethodId,
            TransactionNumber = pr.TransactionNumber,
            PaymentDate = pr.PaymentDate,
            ProofFileUrl = pr.ProofFileUrl,
            Notes = pr.Notes,
            Status = pr.Status,
            ReviewedBy = pr.ReviewedBy,
            ReviewedAt = pr.ReviewedAt,
            RejectReason = pr.RejectReason,
            CreatedAt = pr.CreatedAt
        };
    }
}

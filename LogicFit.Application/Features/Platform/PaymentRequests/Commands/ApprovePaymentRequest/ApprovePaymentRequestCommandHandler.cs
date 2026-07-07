using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.PaymentRequests.Commands.ApprovePaymentRequest;

public class ApprovePaymentRequestCommandHandler : IRequestHandler<ApprovePaymentRequestCommand, PaymentRequestDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly INotificationService _notificationService;

    public ApprovePaymentRequestCommandHandler(
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

    public async Task<PaymentRequestDto> Handle(ApprovePaymentRequestCommand request, CancellationToken cancellationToken)
    {
        var pr = await _context.PaymentRequests
            .Include(p => p.Plan)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentRequestId, cancellationToken);

        if (pr == null)
        {
            throw new NotFoundException(nameof(PaymentRequest), request.PaymentRequestId);
        }

        // Idempotency: a non-pending request is a no-op (protects against double-clicks/retries).
        if (pr.Status != PaymentRequestStatus.Pending)
        {
            return ToDto(pr, pr.Plan?.Name);
        }

        var now = _dateTimeService.UtcNow;
        var reviewer = _currentUserService.UserId;

        pr.Status = PaymentRequestStatus.Approved;
        pr.ReviewedBy = reviewer;
        pr.ReviewedAt = now;

        // Activate or extend the subscription.
        var subscription = pr.TenantSubscriptionId.HasValue
            ? await _context.TenantSubscriptions.FirstOrDefaultAsync(s => s.Id == pr.TenantSubscriptionId.Value, cancellationToken)
            : null;

        if (subscription == null)
        {
            subscription = new TenantSubscription
            {
                TenantId = pr.TenantId,
                PlanId = pr.PlanId,
                BillingCycle = pr.Plan?.BillingCycle ?? BillingCycle.Monthly,
                Amount = pr.Amount,
                Currency = pr.Currency
            };
            _context.TenantSubscriptions.Add(subscription);
            pr.TenantSubscriptionId = subscription.Id;
        }

        var durationDays = pr.Plan?.DurationInDays ?? 30;
        var extendFrom = (subscription.Status == TenantSubscriptionStatus.Active && subscription.EndDate.HasValue && subscription.EndDate > now)
            ? subscription.EndDate.Value
            : now;

        subscription.Status = TenantSubscriptionStatus.Active;
        subscription.StartDate ??= now;
        subscription.EndDate = extendFrom.AddDays(durationDays);
        subscription.RenewDate = subscription.EndDate;
        subscription.ApprovedBy = reviewer;
        subscription.ApprovedAt = now;
        subscription.ReminderSentAt = null; // new period → allow a fresh "expiring soon" reminder

        // Activate the tenant.
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == pr.TenantId, cancellationToken);
        if (tenant != null)
        {
            tenant.Status = TenantStatus.Active;
        }

        // Settled payment + paid invoice.
        var shortId = pr.Id.ToString("N")[..8].ToUpperInvariant();
        _context.SubscriptionPayments.Add(new SubscriptionPayment
        {
            TenantId = pr.TenantId,
            TenantSubscriptionId = subscription.Id,
            PaymentRequestId = pr.Id,
            Amount = pr.Amount,
            Currency = pr.Currency,
            PaymentMethodId = pr.PaymentMethodId,
            TransactionNumber = pr.TransactionNumber,
            PaymentDate = pr.PaymentDate,
            ApprovedBy = reviewer,
            ApprovedAt = now,
            ReceiptNumber = $"RCP-{now:yyyyMMdd}-{shortId}"
        });

        _context.SubscriptionInvoices.Add(new SubscriptionInvoice
        {
            TenantId = pr.TenantId,
            TenantSubscriptionId = subscription.Id,
            InvoiceNumber = $"INV-{now:yyyyMMdd}-{shortId}",
            Amount = pr.Amount,
            Currency = pr.Currency,
            Status = SubscriptionInvoiceStatus.Paid,
            IssueDate = now,
            PaidAt = now
        });

        // Notify the owner (in-app row joins this transaction; email is dispatched best-effort).
        await _notificationService.NotifyTenantOwnerAsync(
            pr.TenantId, Common.Notifications.NotificationTemplates.PaymentRequestApproved, null, cancellationToken);

        // A single SaveChanges is atomic; RowVersion on PaymentRequest/TenantSubscription
        // rejects concurrent approvals with a DbUpdateConcurrencyException.
        await _context.SaveChangesAsync(cancellationToken);

        return ToDto(pr, pr.Plan?.Name);
    }

    private static PaymentRequestDto ToDto(PaymentRequest pr, string? planName) => new()
    {
        Id = pr.Id,
        TenantId = pr.TenantId,
        PlanId = pr.PlanId,
        PlanName = planName,
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

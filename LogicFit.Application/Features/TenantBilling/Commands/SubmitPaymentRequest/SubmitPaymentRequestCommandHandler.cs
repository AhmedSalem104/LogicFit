using LogicFit.Application.Common.Interfaces;
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
            Amount = plan.Price,
            Currency = plan.Currency,
            PaymentMethodId = request.PaymentMethodId,
            TransactionNumber = request.TransactionNumber,
            PaymentDate = request.PaymentDate ?? _dateTimeService.UtcNow,
            ProofFileUrl = request.ProofFileUrl,
            Notes = request.Notes,
            Operation = request.Operation,
            ExtensionDays = request.ExtensionDays,
            Status = PaymentRequestStatus.Pending
        };
        _context.PaymentRequests.Add(paymentRequest);

        await _context.SaveChangesAsync(cancellationToken);

        return new PaymentRequestDto
        {
            Id = paymentRequest.Id,
            TenantId = paymentRequest.TenantId,
            PlanId = paymentRequest.PlanId,
            PlanName = plan.Name,
            TenantSubscriptionId = paymentRequest.TenantSubscriptionId,
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

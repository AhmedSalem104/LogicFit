using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Payments.Commands.RecordPayment;

public class RecordPaymentCommandHandler : IRequestHandler<RecordPaymentCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public RecordPaymentCommandHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUserService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Guid> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new DomainException("Payment amount must be greater than zero");

        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;

        Invoice? invoice = null;
        if (request.InvoiceId.HasValue)
        {
            invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId.Value && i.TenantId == tenantId, cancellationToken)
                ?? throw new NotFoundException("Invoice", request.InvoiceId.Value);

            if (invoice.Status == InvoiceStatus.Cancelled)
                throw new DomainException("Cannot pay a cancelled invoice");
            if (invoice.Status == InvoiceStatus.Draft)
                throw new DomainException("Cannot pay a draft invoice — issue it first");
            if (invoice.RemainingAmount <= 0)
                throw new DomainException("Invoice is already fully paid");
            if (request.Amount > invoice.RemainingAmount)
                throw new DomainException($"Payment exceeds remaining amount ({invoice.RemainingAmount:C})");
        }

        Guid? receivedById = null;
        if (Guid.TryParse(_currentUserService.UserId, out var parsedUserId))
            receivedById = parsedUserId;

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceId = request.InvoiceId,
            SubscriptionId = request.SubscriptionId,
            BranchId = request.BranchId ?? invoice?.BranchId,
            ClientId = request.ClientId ?? invoice?.ClientId,
            Amount = request.Amount,
            Method = request.Method,
            ReceivedAt = request.ReceivedAt ?? now,
            ReceivedById = receivedById,
            ReceiptNumber = request.ReceiptNumber,
            Notes = request.Notes,
            ReferenceNumber = request.ReferenceNumber
        };

        _context.Payments.Add(payment);

        if (invoice != null)
        {
            invoice.AmountPaid += request.Amount;
            if (invoice.AmountPaid >= invoice.Total)
                invoice.Status = InvoiceStatus.Paid;
            else if (invoice.AmountPaid > 0)
                invoice.Status = InvoiceStatus.PartiallyPaid;
        }

        if (request.SubscriptionId.HasValue)
        {
            var subscription = await _context.ClientSubscriptions
                .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId.Value && s.TenantId == tenantId, cancellationToken);

            if (subscription != null)
                subscription.AmountPaid += request.Amount;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return payment.Id;
    }
}

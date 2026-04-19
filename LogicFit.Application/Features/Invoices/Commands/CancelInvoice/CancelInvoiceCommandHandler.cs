using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Invoices.Commands.CancelInvoice;

public class CancelInvoiceCommandHandler : IRequestHandler<CancelInvoiceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CancelInvoiceCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(CancelInvoiceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.Id && i.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Invoice", request.Id);

        if (invoice.Status == InvoiceStatus.Cancelled)
            throw new DomainException("Invoice is already cancelled");
        if (invoice.AmountPaid > 0)
            throw new DomainException("Cannot cancel an invoice that has payments. Refund first.");

        invoice.Status = InvoiceStatus.Cancelled;
        if (!string.IsNullOrEmpty(request.Reason))
            invoice.Notes = (invoice.Notes ?? string.Empty) + $"\n[Cancelled] {request.Reason}";

        await _context.SaveChangesAsync(cancellationToken);
    }
}

using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Invoices.Commands.IssueInvoice;

public class IssueInvoiceCommandHandler : IRequestHandler<IssueInvoiceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public IssueInvoiceCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(IssueInvoiceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.Id && i.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Invoice", request.Id);

        if (invoice.Status != InvoiceStatus.Draft)
            throw new DomainException("Only draft invoices can be issued");

        invoice.Status = InvoiceStatus.Issued;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

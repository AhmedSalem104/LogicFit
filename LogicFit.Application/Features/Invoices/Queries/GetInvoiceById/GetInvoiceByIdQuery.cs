using LogicFit.Application.Features.Invoices.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Invoices.Queries.GetInvoiceById;

public class GetInvoiceByIdQuery : IRequest<InvoiceDto?>
{
    public Guid Id { get; set; }
}

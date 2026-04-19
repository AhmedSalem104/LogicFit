using LogicFit.Application.Features.Invoices.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Invoices.Queries.GetInvoices;

public class GetInvoicesQuery : IRequest<List<InvoiceDto>>
{
    public Guid? ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public InvoiceStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

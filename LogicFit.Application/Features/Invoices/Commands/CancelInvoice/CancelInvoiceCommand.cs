using MediatR;

namespace LogicFit.Application.Features.Invoices.Commands.CancelInvoice;

public class CancelInvoiceCommand : IRequest
{
    public Guid Id { get; set; }
    public string? Reason { get; set; }
}

using MediatR;

namespace LogicFit.Application.Features.Invoices.Commands.IssueInvoice;

public class IssueInvoiceCommand : IRequest
{
    public Guid Id { get; set; }
}

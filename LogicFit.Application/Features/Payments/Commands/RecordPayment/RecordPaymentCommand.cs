using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Payments.Commands.RecordPayment;

public class RecordPaymentCommand : IRequest<Guid>
{
    public Guid? InvoiceId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
    public string? ReferenceNumber { get; set; }
}

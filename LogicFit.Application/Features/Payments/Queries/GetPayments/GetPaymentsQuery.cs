using LogicFit.Application.Features.Payments.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Payments.Queries.GetPayments;

public class GetPaymentsQuery : IRequest<List<PaymentDto>>
{
    public Guid? ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public PaymentMethod? Method { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

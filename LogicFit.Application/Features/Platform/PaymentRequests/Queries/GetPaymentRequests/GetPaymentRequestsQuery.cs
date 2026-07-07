using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Platform.PaymentRequests.Queries.GetPaymentRequests;

public class GetPaymentRequestsQuery : IRequest<List<PaymentRequestDto>>
{
    public PaymentRequestStatus? Status { get; set; }
}

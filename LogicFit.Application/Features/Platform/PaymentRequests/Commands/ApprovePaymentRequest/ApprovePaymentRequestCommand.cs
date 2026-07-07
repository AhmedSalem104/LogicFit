using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Platform.PaymentRequests.Commands.ApprovePaymentRequest;

public class ApprovePaymentRequestCommand : IRequest<PaymentRequestDto>
{
    public Guid PaymentRequestId { get; set; }

    public ApprovePaymentRequestCommand(Guid paymentRequestId) => PaymentRequestId = paymentRequestId;
}

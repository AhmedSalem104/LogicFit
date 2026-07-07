using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Platform.PaymentRequests.Commands.RejectPaymentRequest;

public class RejectPaymentRequestCommand : IRequest<PaymentRequestDto>
{
    public Guid PaymentRequestId { get; set; }
    public string RejectReason { get; set; } = string.Empty;
}

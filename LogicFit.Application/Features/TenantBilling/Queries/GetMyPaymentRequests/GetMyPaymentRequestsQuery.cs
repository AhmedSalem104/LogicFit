using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using MediatR;

namespace LogicFit.Application.Features.TenantBilling.Queries.GetMyPaymentRequests;

public class GetMyPaymentRequestsQuery : IRequest<List<PaymentRequestDto>>
{
}

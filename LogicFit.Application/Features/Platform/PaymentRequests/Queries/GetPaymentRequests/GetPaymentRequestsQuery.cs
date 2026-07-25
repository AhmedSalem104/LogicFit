using LogicFit.Application.Common.Models;
using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Platform.PaymentRequests.Queries.GetPaymentRequests;

public class GetPaymentRequestsQuery : IRequest<PagedResult<PaymentRequestDto>>
{
    public PaymentRequestStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = PageRequest.DefaultPageSize;
}

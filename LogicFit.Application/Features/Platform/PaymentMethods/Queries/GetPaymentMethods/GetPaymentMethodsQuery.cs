using LogicFit.Application.Features.Platform.PaymentMethods.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Platform.PaymentMethods.Queries.GetPaymentMethods;

public class GetPaymentMethodsQuery : IRequest<List<PaymentMethodDto>>
{
    /// <summary>When true, only active methods are returned (tenant-facing list).</summary>
    public bool ActiveOnly { get; set; }
}

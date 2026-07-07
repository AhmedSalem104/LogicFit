using LogicFit.Application.Features.TenantBilling.DTOs;
using MediatR;

namespace LogicFit.Application.Features.TenantBilling.Queries.GetMyInvoices;

public class GetMyInvoicesQuery : IRequest<List<SubscriptionInvoiceDto>>
{
}

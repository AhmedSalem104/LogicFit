using LogicFit.Application.Features.Sales.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Sales.Queries.GetSales;

public class GetSalesQuery : IRequest<List<SaleDto>>
{
    public Guid? BranchId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? CashierId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

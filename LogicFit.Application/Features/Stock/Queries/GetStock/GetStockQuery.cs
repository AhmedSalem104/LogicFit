using LogicFit.Application.Features.Stock.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Stock.Queries.GetStock;

public class GetStockQuery : IRequest<List<StockItemDto>>
{
    public Guid? BranchId { get; set; }
    public Guid? ProductId { get; set; }
    public bool? LowStockOnly { get; set; }
}

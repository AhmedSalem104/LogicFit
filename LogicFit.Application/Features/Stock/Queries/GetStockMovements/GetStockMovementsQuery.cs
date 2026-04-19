using LogicFit.Application.Features.Stock.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Stock.Queries.GetStockMovements;

public class GetStockMovementsQuery : IRequest<List<StockMovementDto>>
{
    public Guid? ProductId { get; set; }
    public Guid? BranchId { get; set; }
    public StockMovementType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

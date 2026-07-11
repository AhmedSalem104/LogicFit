using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Stock.Commands.AdjustStock;

public class AdjustStockCommand : IRequest, IRequireFeature
{
    public string RequiredFeatureCode => FeatureCodes.Inventory;

    public Guid ProductId { get; set; }
    public Guid BranchId { get; set; }
    public StockMovementType Type { get; set; } = StockMovementType.Adjustment;
    public decimal Quantity { get; set; }
    public string? Reason { get; set; }
}

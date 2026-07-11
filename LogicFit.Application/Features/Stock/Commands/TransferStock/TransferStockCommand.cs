using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using MediatR;

namespace LogicFit.Application.Features.Stock.Commands.TransferStock;

public class TransferStockCommand : IRequest, IRequireFeature
{
    public string RequiredFeatureCode => FeatureCodes.Inventory;

    public Guid ProductId { get; set; }
    public Guid FromBranchId { get; set; }
    public Guid ToBranchId { get; set; }
    public decimal Quantity { get; set; }
    public string? Reason { get; set; }
}

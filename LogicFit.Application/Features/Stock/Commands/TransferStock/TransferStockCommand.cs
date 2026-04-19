using MediatR;

namespace LogicFit.Application.Features.Stock.Commands.TransferStock;

public class TransferStockCommand : IRequest
{
    public Guid ProductId { get; set; }
    public Guid FromBranchId { get; set; }
    public Guid ToBranchId { get; set; }
    public decimal Quantity { get; set; }
    public string? Reason { get; set; }
}

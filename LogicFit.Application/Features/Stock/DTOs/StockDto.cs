using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Stock.DTOs;

public class StockItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public Guid BranchId { get; set; }
    public string? BranchName { get; set; }
    public decimal Quantity { get; set; }
    public int MinStockLevel { get; set; }
    public bool IsLowStock => Quantity <= MinStockLevel;
    public DateTime? LastMovementAt { get; set; }
}

public class StockMovementDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public Guid BranchId { get; set; }
    public string? BranchName { get; set; }
    public StockMovementType Type { get; set; }
    public string TypeName => Type.ToString();
    public decimal Quantity { get; set; }
    public decimal QuantityAfter { get; set; }
    public string? Reason { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public DateTime MovedAt { get; set; }
    public string? MovedByName { get; set; }
    public Guid? TargetBranchId { get; set; }
    public string? TargetBranchName { get; set; }
}

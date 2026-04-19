using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class Product : TenantAuditableEntity
{
    public Guid? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal TaxRate { get; set; }
    public string? Unit { get; set; } // piece, kg, liter
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int MinStockLevel { get; set; }
    public bool TrackStock { get; set; } = true;

    public virtual ProductCategory? Category { get; set; }
    public virtual ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
}

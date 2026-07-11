using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using MediatR;

namespace LogicFit.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommand : IRequest<Guid>, IRequireFeature
{
    public string RequiredFeatureCode => FeatureCodes.Inventory;

    public Guid? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal TaxRate { get; set; }
    public string? Unit { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int MinStockLevel { get; set; }
    public bool TrackStock { get; set; } = true;
}

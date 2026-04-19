using LogicFit.Application.Features.Products.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Products.Queries.GetProducts;

public class GetProductsQuery : IRequest<List<ProductDto>>
{
    public Guid? CategoryId { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
    public bool? LowStockOnly { get; set; }
    public Guid? BranchId { get; set; } // scope stock calculation to branch
}

using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Products.Queries.GetProducts;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetProductsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.StockItems)
            .Where(p => p.TenantId == tenantId)
            .AsQueryable();

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        if (request.IsActive.HasValue)
            query = query.Where(p => p.IsActive == request.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(p => p.Name.Contains(term)
                || (p.Sku != null && p.Sku.Contains(term))
                || (p.Barcode != null && p.Barcode.Contains(term)));
        }

        var products = await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);

        var result = products.Select(p => new ProductDto
        {
            Id = p.Id,
            TenantId = p.TenantId,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name,
            Name = p.Name,
            Description = p.Description,
            Sku = p.Sku,
            Barcode = p.Barcode,
            CostPrice = p.CostPrice,
            SellingPrice = p.SellingPrice,
            TaxRate = p.TaxRate,
            Unit = p.Unit,
            ImageUrl = p.ImageUrl,
            IsActive = p.IsActive,
            MinStockLevel = p.MinStockLevel,
            TrackStock = p.TrackStock,
            TotalStock = request.BranchId.HasValue
                ? p.StockItems.Where(s => s.BranchId == request.BranchId.Value && !s.IsDeleted).Sum(s => s.Quantity)
                : p.StockItems.Where(s => !s.IsDeleted).Sum(s => s.Quantity)
        }).ToList();

        if (request.LowStockOnly == true)
            result = result.Where(p => p.IsLowStock).ToList();

        return result;
    }
}

using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetStockValuationReport;

public class GetStockValuationReportQueryHandler : IRequestHandler<GetStockValuationReportQuery, StockValuationReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetStockValuationReportQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<StockValuationReportDto> Handle(GetStockValuationReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.StockItems
            .Include(s => s.Product)
            .Include(s => s.Branch)
            .Where(s => s.TenantId == tenantId && s.Product.TrackStock);

        if (request.BranchId.HasValue)
            query = query.Where(s => s.BranchId == request.BranchId.Value);

        var stocks = await query.ToListAsync(cancellationToken);

        // If a specific branch: aggregate by product in that branch. Otherwise aggregate per product-branch.
        var products = stocks
            .GroupBy(s => new { s.ProductId, s.Product.Name, s.Product.Sku, s.Product.CostPrice, s.Product.SellingPrice, s.Product.MinStockLevel })
            .Select(g => new StockProductValuationDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                Sku = g.Key.Sku,
                Quantity = g.Sum(s => s.Quantity),
                CostPrice = g.Key.CostPrice,
                SellingPrice = g.Key.SellingPrice,
                CostValue = g.Sum(s => s.Quantity) * g.Key.CostPrice,
                RetailValue = g.Sum(s => s.Quantity) * g.Key.SellingPrice,
                MinStockLevel = g.Key.MinStockLevel,
                IsLowStock = g.Sum(s => s.Quantity) <= g.Key.MinStockLevel
            })
            .OrderByDescending(p => p.CostValue)
            .ToList();

        string? branchName = null;
        if (request.BranchId.HasValue)
            branchName = stocks.FirstOrDefault()?.Branch.Name;

        return new StockValuationReportDto
        {
            BranchId = request.BranchId,
            BranchName = branchName,
            TotalCostValue = products.Sum(p => p.CostValue),
            TotalRetailValue = products.Sum(p => p.RetailValue),
            ProductsCount = products.Count,
            LowStockCount = products.Count(p => p.IsLowStock),
            Products = products
        };
    }
}

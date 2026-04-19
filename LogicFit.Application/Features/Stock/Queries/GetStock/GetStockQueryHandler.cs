using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Stock.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Stock.Queries.GetStock;

public class GetStockQueryHandler : IRequestHandler<GetStockQuery, List<StockItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetStockQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<StockItemDto>> Handle(GetStockQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.StockItems
            .Include(s => s.Product)
            .Include(s => s.Branch)
            .Where(s => s.TenantId == tenantId)
            .AsQueryable();

        if (request.BranchId.HasValue)
            query = query.Where(s => s.BranchId == request.BranchId.Value);
        if (request.ProductId.HasValue)
            query = query.Where(s => s.ProductId == request.ProductId.Value);

        var items = await query.OrderBy(s => s.Product.Name).ToListAsync(cancellationToken);

        var result = items.Select(s => new StockItemDto
        {
            Id = s.Id,
            ProductId = s.ProductId,
            ProductName = s.Product.Name,
            Sku = s.Product.Sku,
            BranchId = s.BranchId,
            BranchName = s.Branch.Name,
            Quantity = s.Quantity,
            MinStockLevel = s.Product.MinStockLevel,
            LastMovementAt = s.LastMovementAt
        }).ToList();

        if (request.LowStockOnly == true)
            result = result.Where(x => x.IsLowStock).ToList();

        return result;
    }
}

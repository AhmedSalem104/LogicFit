using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Stock.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Stock.Queries.GetStockMovements;

public class GetStockMovementsQueryHandler : IRequestHandler<GetStockMovementsQuery, List<StockMovementDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetStockMovementsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<StockMovementDto>> Handle(GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.StockMovements
            .Include(m => m.Product)
            .Include(m => m.Branch)
            .Include(m => m.MovedBy)
            .Include(m => m.TargetBranch)
            .Where(m => m.TenantId == tenantId)
            .AsQueryable();

        if (request.ProductId.HasValue)
            query = query.Where(m => m.ProductId == request.ProductId.Value);
        if (request.BranchId.HasValue)
            query = query.Where(m => m.BranchId == request.BranchId.Value);
        if (request.Type.HasValue)
            query = query.Where(m => m.Type == request.Type.Value);
        if (request.FromDate.HasValue)
            query = query.Where(m => m.MovedAt >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(m => m.MovedAt <= request.ToDate.Value);

        var movements = await query.OrderByDescending(m => m.MovedAt).Take(500).ToListAsync(cancellationToken);

        return movements.Select(m => new StockMovementDto
        {
            Id = m.Id,
            ProductId = m.ProductId,
            ProductName = m.Product.Name,
            BranchId = m.BranchId,
            BranchName = m.Branch.Name,
            Type = m.Type,
            Quantity = m.Quantity,
            QuantityAfter = m.QuantityAfter,
            Reason = m.Reason,
            ReferenceType = m.ReferenceType,
            ReferenceId = m.ReferenceId,
            MovedAt = m.MovedAt,
            MovedByName = m.MovedBy?.Email,
            TargetBranchId = m.TargetBranchId,
            TargetBranchName = m.TargetBranch?.Name
        }).ToList();
    }
}

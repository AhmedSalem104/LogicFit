using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Stock.Commands.AdjustStock;

public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public AdjustStockCommandHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUserService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");

        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Product", request.ProductId);

        var branchExists = await _context.Branches.AnyAsync(b => b.Id == request.BranchId && b.TenantId == tenantId, cancellationToken);
        if (!branchExists)
            throw new NotFoundException("Branch", request.BranchId);

        var stockItem = await _context.StockItems
            .FirstOrDefaultAsync(s => s.ProductId == request.ProductId && s.BranchId == request.BranchId && s.TenantId == tenantId, cancellationToken);

        if (stockItem == null)
        {
            stockItem = new StockItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProductId = request.ProductId,
                BranchId = request.BranchId,
                Quantity = 0
            };
            _context.StockItems.Add(stockItem);
        }

        var isIn = request.Type == StockMovementType.In || (request.Type == StockMovementType.Adjustment);
        if (request.Type == StockMovementType.Out)
        {
            if (stockItem.Quantity < request.Quantity)
                throw new DomainException($"Insufficient stock. Available: {stockItem.Quantity}");
            stockItem.Quantity -= request.Quantity;
        }
        else if (request.Type == StockMovementType.In)
        {
            stockItem.Quantity += request.Quantity;
        }
        else if (request.Type == StockMovementType.Adjustment)
        {
            // Adjustment: quantity is the new absolute value
            stockItem.Quantity = request.Quantity;
        }
        else
        {
            throw new DomainException("Use transfer endpoint for Transfer type");
        }

        stockItem.LastMovementAt = now;

        Guid? userId = null;
        if (Guid.TryParse(_currentUserService.UserId, out var uid)) userId = uid;

        _context.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = request.ProductId,
            BranchId = request.BranchId,
            Type = request.Type,
            Quantity = request.Quantity,
            QuantityAfter = stockItem.Quantity,
            Reason = request.Reason,
            ReferenceType = "Manual",
            MovedAt = now,
            MovedById = userId
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}

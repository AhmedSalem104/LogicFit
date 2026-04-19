using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Stock.Commands.TransferStock;

public class TransferStockCommandHandler : IRequestHandler<TransferStockCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public TransferStockCommandHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUserService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(TransferStockCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");
        if (request.FromBranchId == request.ToBranchId)
            throw new DomainException("Source and destination branches cannot be the same");

        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;

        var source = await _context.StockItems
            .FirstOrDefaultAsync(s => s.ProductId == request.ProductId && s.BranchId == request.FromBranchId && s.TenantId == tenantId, cancellationToken)
            ?? throw new DomainException("Source branch has no stock for this product");

        if (source.Quantity < request.Quantity)
            throw new DomainException($"Insufficient stock in source branch. Available: {source.Quantity}");

        source.Quantity -= request.Quantity;
        source.LastMovementAt = now;

        var destination = await _context.StockItems
            .FirstOrDefaultAsync(s => s.ProductId == request.ProductId && s.BranchId == request.ToBranchId && s.TenantId == tenantId, cancellationToken);

        if (destination == null)
        {
            destination = new StockItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProductId = request.ProductId,
                BranchId = request.ToBranchId,
                Quantity = 0
            };
            _context.StockItems.Add(destination);
        }

        destination.Quantity += request.Quantity;
        destination.LastMovementAt = now;

        Guid? userId = null;
        if (Guid.TryParse(_currentUserService.UserId, out var uid)) userId = uid;

        _context.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = request.ProductId,
            BranchId = request.FromBranchId,
            Type = StockMovementType.Transfer,
            Quantity = request.Quantity,
            QuantityAfter = source.Quantity,
            Reason = request.Reason,
            ReferenceType = "Transfer",
            MovedAt = now,
            MovedById = userId,
            TargetBranchId = request.ToBranchId
        });

        _context.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = request.ProductId,
            BranchId = request.ToBranchId,
            Type = StockMovementType.In,
            Quantity = request.Quantity,
            QuantityAfter = destination.Quantity,
            Reason = "Transfer-in from branch " + request.FromBranchId,
            ReferenceType = "Transfer",
            MovedAt = now,
            MovedById = userId
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}

using System.Data;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
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

        await using var dbTransaction = await _context.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        async Task<decimal> DecreaseSourceAsync()
        {
            var result = await StockConcurrencyOperations.TryDecreaseExistingAsync(
                _context,
                tenantId,
                request.ProductId,
                request.FromBranchId,
                request.Quantity,
                now,
                cancellationToken);

            if (result.HasValue)
                return result.Value;

            var available = await _context.StockItems
                .Where(s => s.ProductId == request.ProductId &&
                            s.BranchId == request.FromBranchId &&
                            s.TenantId == tenantId)
                .Select(s => (decimal?)s.Quantity)
                .SingleOrDefaultAsync(cancellationToken);
            if (!available.HasValue)
                throw new DomainException("Source branch has no stock for this product");

            throw new DomainException($"Insufficient stock in source branch. Available: {available.Value}");
        }

        async Task<decimal> IncreaseDestinationAsync()
        {
            var result = await StockConcurrencyOperations.TryIncreaseExistingAsync(
                _context,
                tenantId,
                request.ProductId,
                request.ToBranchId,
                request.Quantity,
                now,
                cancellationToken);

            if (result.HasValue)
                return result.Value;

            _context.StockItems.Add(new StockItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProductId = request.ProductId,
                BranchId = request.ToBranchId,
                Quantity = request.Quantity,
                LastMovementAt = now
            });

            return request.Quantity;
        }

        // Lock both branch rows in a stable order so opposing transfers cannot deadlock.
        decimal sourceAfter;
        decimal destinationAfter;
        if (request.FromBranchId.CompareTo(request.ToBranchId) < 0)
        {
            sourceAfter = await DecreaseSourceAsync();
            destinationAfter = await IncreaseDestinationAsync();
        }
        else
        {
            destinationAfter = await IncreaseDestinationAsync();
            sourceAfter = await DecreaseSourceAsync();
        }

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
            QuantityAfter = sourceAfter,
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
            QuantityAfter = destinationAfter,
            Reason = "Transfer-in from branch " + request.FromBranchId,
            ReferenceType = "Transfer",
            MovedAt = now,
            MovedById = userId
        });

        await _context.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);
    }
}

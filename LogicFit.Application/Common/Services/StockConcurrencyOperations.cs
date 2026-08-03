using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Common.Services;

/// <summary>
/// Performs guarded SQL changes for an existing stock row and returns its post-update quantity.
/// Missing rows are handled by the caller inside a serializable transaction.
/// </summary>
public static class StockConcurrencyOperations
{
    public static async Task<decimal?> TryIncreaseExistingAsync(
        IApplicationDbContext context,
        Guid tenantId,
        Guid productId,
        Guid branchId,
        decimal quantity,
        DateTime movedAt,
        CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity);

        var affected = await ExistingRows(context, tenantId, productId, branchId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.Quantity, item => item.Quantity + quantity)
                    .SetProperty(item => item.LastMovementAt, movedAt),
                cancellationToken);

        return affected == 0
            ? null
            : await ReadQuantityAsync(context, tenantId, productId, branchId, cancellationToken);
    }

    public static async Task<decimal?> TryDecreaseExistingAsync(
        IApplicationDbContext context,
        Guid tenantId,
        Guid productId,
        Guid branchId,
        decimal quantity,
        DateTime movedAt,
        CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity);

        var affected = await ExistingRows(context, tenantId, productId, branchId)
            .Where(item => item.Quantity >= quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.Quantity, item => item.Quantity - quantity)
                    .SetProperty(item => item.LastMovementAt, movedAt),
                cancellationToken);

        return affected == 0
            ? null
            : await ReadQuantityAsync(context, tenantId, productId, branchId, cancellationToken);
    }

    public static async Task<decimal?> TrySetExistingAsync(
        IApplicationDbContext context,
        Guid tenantId,
        Guid productId,
        Guid branchId,
        decimal quantity,
        DateTime movedAt,
        CancellationToken cancellationToken = default)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Stock quantity cannot be negative.");

        var affected = await ExistingRows(context, tenantId, productId, branchId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.Quantity, quantity)
                    .SetProperty(item => item.LastMovementAt, movedAt),
                cancellationToken);

        return affected == 0
            ? null
            : await ReadQuantityAsync(context, tenantId, productId, branchId, cancellationToken);
    }

    private static IQueryable<StockItem> ExistingRows(
        IApplicationDbContext context,
        Guid tenantId,
        Guid productId,
        Guid branchId) => context.StockItems.Where(item =>
        item.TenantId == tenantId &&
        item.ProductId == productId &&
        item.BranchId == branchId &&
        !item.IsDeleted);

    private static Task<decimal?> ReadQuantityAsync(
        IApplicationDbContext context,
        Guid tenantId,
        Guid productId,
        Guid branchId,
        CancellationToken cancellationToken) => ExistingRows(context, tenantId, productId, branchId)
        .Select(item => (decimal?)item.Quantity)
        .SingleOrDefaultAsync(cancellationToken);

    private static void ValidateQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Stock quantity must be greater than zero.");
    }
}

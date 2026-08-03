using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Common.Services;

/// <summary>
/// Applies wallet changes as guarded SQL updates. Callers must run the update and its ledger
/// insert in the same database transaction.
/// </summary>
public static class WalletBalanceOperations
{
    public static async Task<decimal> ApplyAsync(
        IApplicationDbContext context,
        Guid tenantId,
        Guid userId,
        decimal delta,
        CancellationToken cancellationToken = default,
        string validationKey = "Amount")
    {
        var users = context.Users
            .Where(user => user.Id == userId && user.TenantId == tenantId && !user.IsDeleted);

        if (delta == 0)
            return await users.Select(user => (decimal?)user.WalletBalance).SingleOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("User", userId);

        if (delta < 0)
            users = users.Where(user => user.WalletBalance >= -delta);

        var affected = await users.ExecuteUpdateAsync(
            setters => setters.SetProperty(user => user.WalletBalance, user => user.WalletBalance + delta),
            cancellationToken);

        if (affected == 0)
        {
            var exists = await context.Users.AnyAsync(
                user => user.Id == userId && user.TenantId == tenantId && !user.IsDeleted,
                cancellationToken);

            if (!exists)
                throw new NotFoundException("User", userId);

            throw new ValidationException(validationKey, "Insufficient wallet balance");
        }

        return await context.Users
            .Where(user => user.Id == userId && user.TenantId == tenantId && !user.IsDeleted)
            .Select(user => user.WalletBalance)
            .SingleAsync(cancellationToken);
    }
}

using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Interfaces;

/// <summary>
/// Evaluates the tenant's active <c>CommissionRule</c>s for a completed sale and stages a pending
/// <c>Commission</c> for the selling employee. It only ADDs to the DbContext — the caller owns the
/// <c>SaveChangesAsync</c> so the commission is persisted in the same transaction as the sale.
/// </summary>
public interface ICommissionService
{
    /// <summary>
    /// Accrues a commission for the user who made the sale, if a matching rule exists.
    /// No-op when the seller is unknown, is not an employee, the amount is non-positive, or no active
    /// rule matches. Returns the accrued amount (0 when nothing was staged).
    /// </summary>
    /// <param name="sellerUserId">The <c>User.Id</c> of the seller (cashier / sales coach).</param>
    /// <param name="sourceType">What was sold (ProductSale / SubscriptionSale / ...).</param>
    /// <param name="sourceAmount">The amount the commission is computed from (net sale/subscription total).</param>
    /// <param name="referenceId">The originating Sale / Subscription id, for traceability.</param>
    Task<decimal> AccrueAsync(
        Guid tenantId,
        Guid? sellerUserId,
        CommissionSourceType sourceType,
        decimal sourceAmount,
        Guid referenceId,
        DateTime earnedDate,
        string? description,
        CancellationToken cancellationToken = default);
}

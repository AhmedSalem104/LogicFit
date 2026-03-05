using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Commands.CancelSubscription;

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CancelSubscriptionCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var subscription = await _context.ClientSubscriptions
            .Include(s => s.Client)
            .Include(s => s.Plan)
            .Include(s => s.Freezes)
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId && s.TenantId == tenantId, cancellationToken);

        if (subscription == null)
            throw new NotFoundException("ClientSubscription", request.SubscriptionId);

        // Handle refund
        if (request.RefundToWallet && subscription.AmountPaid > 0)
        {
            decimal refundAmount;

            if (request.RefundAmount.HasValue && request.RefundAmount.Value > 0)
            {
                refundAmount = Math.Min(request.RefundAmount.Value, subscription.AmountPaid);
            }
            else
            {
                // Calculate proportional refund based on remaining days
                var totalDays = (subscription.EndDate - subscription.StartDate).TotalDays;
                var remainingDays = Math.Max(0, (subscription.EndDate - DateTime.UtcNow).TotalDays);
                refundAmount = totalDays > 0
                    ? Math.Round(subscription.AmountPaid * (decimal)(remainingDays / totalDays), 2)
                    : 0;
            }

            if (refundAmount > 0)
            {
                subscription.Client.WalletBalance += refundAmount;

                var transaction = new WalletTransaction
                {
                    TenantId = tenantId,
                    UserId = subscription.ClientId,
                    Type = TransactionType.Refund,
                    Amount = refundAmount,
                    BalanceAfter = subscription.Client.WalletBalance,
                    Description = $"Subscription refund - {subscription.Plan.Name}",
                    ReferenceType = "Subscription",
                    ReferenceId = subscription.Id
                };
                _context.WalletTransactions.Add(transaction);
            }
        }

        subscription.Status = SubscriptionStatus.Cancelled;

        // Deactivate any active freezes
        foreach (var freeze in subscription.Freezes.Where(f => f.IsActive))
        {
            freeze.IsActive = false;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

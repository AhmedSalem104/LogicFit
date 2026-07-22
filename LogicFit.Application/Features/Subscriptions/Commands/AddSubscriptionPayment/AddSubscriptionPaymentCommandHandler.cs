using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Commands.AddSubscriptionPayment;

public class AddSubscriptionPaymentCommandHandler : IRequestHandler<AddSubscriptionPaymentCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public AddSubscriptionPaymentCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(AddSubscriptionPaymentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var subscription = await _context.ClientSubscriptions
            .Include(s => s.Client)
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId && s.TenantId == tenantId, cancellationToken);

        if (subscription == null)
            throw new NotFoundException("ClientSubscription", request.SubscriptionId);

        var remaining = Math.Max(0m, subscription.TotalAmount - subscription.AmountPaid);
        if (request.Amount > remaining)
            throw new ValidationException("Amount", $"Payment exceeds the remaining subscription balance ({remaining:0.##})");

        if (request.PayFromWallet)
        {
            if (subscription.Client.WalletBalance < request.Amount)
                throw new ValidationException("PayFromWallet", "Insufficient wallet balance");

            subscription.Client.WalletBalance -= request.Amount;

            var transaction = new WalletTransaction
            {
                TenantId = tenantId,
                UserId = subscription.ClientId,
                Type = TransactionType.Payment,
                Amount = request.Amount,
                BalanceAfter = subscription.Client.WalletBalance,
                Description = $"Subscription payment - {subscription.Plan.Name}",
                ReferenceType = "Subscription",
                ReferenceId = subscription.Id
            };
            _context.WalletTransactions.Add(transaction);
        }

        subscription.AmountPaid += request.Amount;
        subscription.PaymentMethod = request.PayFromWallet ? Domain.Enums.PaymentMethod.Wallet : request.PaymentMethod;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

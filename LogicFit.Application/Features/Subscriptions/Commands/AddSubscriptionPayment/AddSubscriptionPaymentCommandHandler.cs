using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
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
    private readonly ICurrentUserService _currentUserService;

    public AddSubscriptionPaymentCommandHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(AddSubscriptionPaymentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var subscription = await _context.ClientSubscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId && s.TenantId == tenantId, cancellationToken);

        if (subscription == null)
            throw new NotFoundException("ClientSubscription", request.SubscriptionId);

        var remaining = Math.Max(0m, subscription.TotalAmount - subscription.AmountPaid);
        if (request.Amount > remaining)
            throw new ValidationException("Amount", $"Payment exceeds the remaining subscription balance ({remaining:0.##})");

        await using var dbTransaction = await _context.BeginTransactionAsync(cancellationToken);

        if (request.PayFromWallet)
        {
            var balanceAfter = await WalletBalanceOperations.ApplyAsync(
                _context,
                tenantId,
                subscription.ClientId,
                -request.Amount,
                cancellationToken,
                validationKey: "PayFromWallet");

            var transaction = new WalletTransaction
            {
                TenantId = tenantId,
                UserId = subscription.ClientId,
                Type = TransactionType.Payment,
                Amount = request.Amount,
                BalanceAfter = balanceAfter,
                Description = $"Subscription payment - {subscription.Plan.Name}",
                ReferenceType = "Subscription",
                ReferenceId = subscription.Id
            };
            _context.WalletTransactions.Add(transaction);
        }

        subscription.AmountPaid += request.Amount;
        subscription.PaymentMethod = request.PayFromWallet ? Domain.Enums.PaymentMethod.Wallet : request.PaymentMethod;
        SubscriptionPaymentLedger.Append(
            _context,
            tenantId,
            subscription,
            request.Amount,
            subscription.PaymentMethod ?? Domain.Enums.PaymentMethod.Cash,
            Guid.TryParse(_currentUserService.UserId, out var receivedById) ? receivedById : null,
            DateTime.UtcNow,
            "Subscription payment");

        await _context.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        return true;
    }
}

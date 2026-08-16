using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Commands.RenewSubscription;

public class RenewSubscriptionCommandHandler : IRequestHandler<RenewSubscriptionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public RenewSubscriptionCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(RenewSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Load existing subscription
        var oldSubscription = await _context.ClientSubscriptions
            .Include(s => s.Freezes)
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId && s.TenantId == tenantId, cancellationToken);

        if (oldSubscription == null)
            throw new NotFoundException("ClientSubscription", request.SubscriptionId);

        // Determine plan
        var planId = request.PlanId ?? oldSubscription.PlanId;
        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == planId && p.TenantId == tenantId, cancellationToken);

        if (plan == null)
            throw new NotFoundException("SubscriptionPlan", planId);

        if (!plan.IsActive)
            throw new ValidationException("PlanId", "This plan is not active");

        // Determine start date
        var today = DateTime.UtcNow.Date;
        if (oldSubscription.Freezes.Any(f => !f.IsDeleted && f.IsActive && f.StartDate.Date <= today && f.EndDate.Date >= today))
            throw new ConflictException("A subscription cannot be renewed while it is currently frozen. Resume it first.");
        var startDate = (request.StartDate?.Date)
            ?? (oldSubscription.EndDate.Date >= today ? oldSubscription.EndDate.Date.AddDays(1) : today);

        // Calculate amounts
        var discount = request.Discount ?? 0;
        var totalAmount = plan.Price - discount;
        if (totalAmount < 0) totalAmount = 0;

        var amountPaid = request.AmountPaid ?? 0;
        if (amountPaid > totalAmount)
            throw new ValidationException("AmountPaid", "Amount paid cannot exceed the renewal total after discount.");
        var paymentMethod = request.PaymentMethod;
        Guid? sellerUserId = Guid.TryParse(_currentUserService.UserId, out var parsedSellerId) ? parsedSellerId : null;
        if (sellerUserId.HasValue && !await _context.Users.AnyAsync(u => u.Id == sellerUserId.Value && u.TenantId == tenantId && !u.IsDeleted, cancellationToken))
            sellerUserId = null;

        await using var dbTransaction = await _context.BeginTransactionAsync(cancellationToken);

        // Handle wallet payment
        if (request.PayFromWallet)
        {
            var walletPayAmount = amountPaid > 0 ? amountPaid : totalAmount;
            var balanceAfter = await WalletBalanceOperations.ApplyAsync(
                _context,
                tenantId,
                oldSubscription.ClientId,
                -walletPayAmount,
                cancellationToken,
                validationKey: "PayFromWallet");
            amountPaid = walletPayAmount;
            paymentMethod = Domain.Enums.PaymentMethod.Wallet;

            var transaction = new WalletTransaction
            {
                TenantId = tenantId,
                UserId = oldSubscription.ClientId,
                Type = TransactionType.Payment,
                Amount = walletPayAmount,
                BalanceAfter = balanceAfter,
                Description = $"Subscription renewal - {plan.Name}",
                ReferenceType = "Subscription"
            };
            _context.WalletTransactions.Add(transaction);
        }

        // Mark old subscription as expired if still active
        if (oldSubscription.Status == SubscriptionStatus.Active || oldSubscription.Status == SubscriptionStatus.Suspended)
        {
            oldSubscription.Status = SubscriptionStatus.Expired;
        }

        // Create new subscription
        var newSubscription = new ClientSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = oldSubscription.ClientId,
            PlanId = planId,
            StartDate = startDate,
            EndDate = startDate.AddMonths(plan.DurationMonths).AddDays(-1),
            Status = SubscriptionStatus.Active,
            SalesCoachId = sellerUserId,
            PaymentMethod = amountPaid > 0 ? paymentMethod ?? Domain.Enums.PaymentMethod.Cash : paymentMethod,
            TotalAmount = totalAmount,
            AmountPaid = amountPaid,
            Discount = discount,
            Notes = request.Notes,
            RenewedFromId = oldSubscription.Id
        };

        _context.ClientSubscriptions.Add(newSubscription);
        SubscriptionPaymentLedger.Append(
            _context,
            tenantId,
            newSubscription,
            amountPaid,
            paymentMethod ?? Domain.Enums.PaymentMethod.Cash,
            sellerUserId,
            DateTime.UtcNow,
            "Subscription renewed");
        await _context.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        return newSubscription.Id;
    }
}

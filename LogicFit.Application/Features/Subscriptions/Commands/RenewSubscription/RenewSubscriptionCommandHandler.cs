using LogicFit.Application.Common.Interfaces;
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
            .Include(s => s.Client)
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
        var startDate = request.StartDate
            ?? (oldSubscription.EndDate > DateTime.UtcNow ? oldSubscription.EndDate : DateTime.UtcNow);

        // Calculate amounts
        var discount = request.Discount ?? 0;
        var totalAmount = plan.Price - discount;
        if (totalAmount < 0) totalAmount = 0;

        var amountPaid = request.AmountPaid ?? 0;
        var paymentMethod = request.PaymentMethod;

        // Handle wallet payment
        if (request.PayFromWallet)
        {
            var walletPayAmount = amountPaid > 0 ? amountPaid : totalAmount;
            var client = oldSubscription.Client;

            if (client.WalletBalance < walletPayAmount)
                throw new ValidationException("PayFromWallet", "Insufficient wallet balance");

            client.WalletBalance -= walletPayAmount;
            amountPaid = walletPayAmount;
            paymentMethod = Domain.Enums.PaymentMethod.Wallet;

            var transaction = new WalletTransaction
            {
                TenantId = tenantId,
                UserId = oldSubscription.ClientId,
                Type = TransactionType.Payment,
                Amount = walletPayAmount,
                BalanceAfter = client.WalletBalance,
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
            EndDate = startDate.AddMonths(plan.DurationMonths),
            Status = SubscriptionStatus.Active,
            SalesCoachId = Guid.Parse(_currentUserService.UserId!),
            PaymentMethod = paymentMethod,
            TotalAmount = totalAmount,
            AmountPaid = amountPaid,
            Discount = discount,
            Notes = request.Notes,
            RenewedFromId = oldSubscription.Id
        };

        _context.ClientSubscriptions.Add(newSubscription);
        await _context.SaveChangesAsync(cancellationToken);

        return newSubscription.Id;
    }
}

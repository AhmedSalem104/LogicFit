using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Commands.CreateClientSubscription;

public class CreateClientSubscriptionCommandHandler : IRequestHandler<CreateClientSubscriptionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public CreateClientSubscriptionCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateClientSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Validate plan exists and is active
        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && p.TenantId == tenantId, cancellationToken);

        if (plan == null)
            throw new NotFoundException("SubscriptionPlan", request.PlanId);

        if (!plan.IsActive)
            throw new ValidationException("PlanId", "This plan is not active");

        // Validate client exists
        var client = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.ClientId && u.TenantId == tenantId && u.Role == UserRole.Client, cancellationToken);

        if (client == null)
            throw new NotFoundException("Client", request.ClientId);

        // Check no overlapping active/suspended subscription
        var hasOverlapping = await _context.ClientSubscriptions
            .AnyAsync(s => s.ClientId == request.ClientId && s.TenantId == tenantId
                && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Suspended)
                && s.EndDate > DateTime.UtcNow, cancellationToken);

        if (hasOverlapping)
            throw new ConflictException("Client already has an active subscription");

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

            if (client.WalletBalance < walletPayAmount)
                throw new ValidationException("PayFromWallet", "Insufficient wallet balance");

            client.WalletBalance -= walletPayAmount;
            amountPaid = walletPayAmount;
            paymentMethod = Domain.Enums.PaymentMethod.Wallet;

            // Create wallet transaction
            var transaction = new WalletTransaction
            {
                TenantId = tenantId,
                UserId = request.ClientId,
                Type = TransactionType.Payment,
                Amount = walletPayAmount,
                BalanceAfter = client.WalletBalance,
                Description = $"Subscription payment - {plan.Name}",
                ReferenceType = "Subscription"
            };
            _context.WalletTransactions.Add(transaction);
        }

        var subscription = new ClientSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = request.ClientId,
            PlanId = request.PlanId,
            StartDate = request.StartDate,
            EndDate = request.StartDate.AddMonths(plan.DurationMonths),
            Status = SubscriptionStatus.Active,
            SalesCoachId = Guid.Parse(_currentUserService.UserId!),
            PaymentMethod = paymentMethod,
            TotalAmount = totalAmount,
            AmountPaid = amountPaid,
            Discount = discount,
            Notes = request.Notes
        };

        _context.ClientSubscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        return subscription.Id;
    }
}

using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
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
    private readonly ICommissionService _commissionService;

    public CreateClientSubscriptionCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        ICommissionService commissionService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _commissionService = commissionService;
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
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.ClientId && u.TenantId == tenantId && u.Role == UserRole.Client, cancellationToken);

        if (client == null)
            throw new NotFoundException("Client", request.ClientId);

        // Check no overlapping active/suspended subscription
        var hasOverlapping = await _context.ClientSubscriptions
            .AnyAsync(s => s.ClientId == request.ClientId && s.TenantId == tenantId
                && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Suspended)
                && s.EndDate.Date >= DateTime.UtcNow.Date, cancellationToken);

        if (hasOverlapping)
            throw new ConflictException("Client already has an active subscription");

        // Calculate amounts
        var discount = request.Discount ?? 0;
        var totalAmount = plan.Price - discount;
        if (totalAmount < 0) totalAmount = 0;

        var amountPaid = request.AmountPaid ?? 0;
        if (amountPaid > totalAmount)
            throw new ValidationException("AmountPaid", "Amount paid cannot exceed the subscription total after discount.");
        var paymentMethod = request.PaymentMethod;
        Guid? sellerUserId = Guid.TryParse(_currentUserService.UserId, out var sellerId)
            ? sellerId
            : null;
        if (sellerUserId.HasValue && !await _context.Users.AnyAsync(
                u => u.Id == sellerUserId.Value && u.TenantId == tenantId && !u.IsDeleted,
                cancellationToken))
        {
            // A token can outlive a workspace membership migration. Do not turn a
            // non-critical commission link into a foreign-key/HTTP 500 failure.
            sellerUserId = null;
        }

        var dbTransaction = request.UseExistingTransaction
            ? null
            : await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            // Handle wallet payment
            if (request.PayFromWallet)
            {
                var walletPayAmount = amountPaid > 0 ? amountPaid : totalAmount;
                var balanceAfter = await WalletBalanceOperations.ApplyAsync(
                    _context,
                    tenantId,
                    request.ClientId,
                    -walletPayAmount,
                    cancellationToken,
                    validationKey: "PayFromWallet");
                amountPaid = walletPayAmount;
                paymentMethod = Domain.Enums.PaymentMethod.Wallet;

                // Create wallet transaction
                var transaction = new WalletTransaction
                {
                    TenantId = tenantId,
                    UserId = request.ClientId,
                    Type = TransactionType.Payment,
                    Amount = walletPayAmount,
                    BalanceAfter = balanceAfter,
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
                EndDate = request.StartDate.Date.AddMonths(plan.DurationMonths).AddDays(-1),
                Status = SubscriptionStatus.Active,
                SalesCoachId = sellerUserId,
                PaymentMethod = amountPaid > 0 ? paymentMethod ?? Domain.Enums.PaymentMethod.Cash : paymentMethod,
                TotalAmount = totalAmount,
                AmountPaid = amountPaid,
                Discount = discount,
                Notes = request.Notes
            };

            _context.ClientSubscriptions.Add(subscription);
            SubscriptionPaymentLedger.Append(
                _context,
                tenantId,
                subscription,
                amountPaid,
                paymentMethod ?? Domain.Enums.PaymentMethod.Cash,
                sellerUserId,
                DateTime.UtcNow,
                "Subscription created");

            // Accrue a sales commission for the selling staff/coach (staged on the same transaction).
            await _commissionService.AccrueAsync(
                tenantId, sellerUserId, CommissionSourceType.SubscriptionSale, totalAmount, subscription.Id,
                DateTime.UtcNow, $"Commission for subscription {subscription.Id}", cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            if (dbTransaction is not null)
                await dbTransaction.CommitAsync(cancellationToken);

            return subscription.Id;
        }
        catch
        {
            if (dbTransaction is not null)
                await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (dbTransaction is not null)
                await dbTransaction.DisposeAsync();
        }
    }
}

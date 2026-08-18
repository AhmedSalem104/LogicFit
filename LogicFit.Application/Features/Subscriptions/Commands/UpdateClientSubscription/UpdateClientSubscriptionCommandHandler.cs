using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Reports.Services;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Commands.UpdateClientSubscription;

public class UpdateClientSubscriptionCommandHandler : IRequestHandler<UpdateClientSubscriptionCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateClientSubscriptionCommandHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(UpdateClientSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var subscription = await _context.ClientSubscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.TenantId == tenantId, cancellationToken);

        if (subscription == null)
            throw new NotFoundException("ClientSubscription", request.Id);

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        if (request.EndDate.HasValue && request.EndDate.Value.Date < subscription.StartDate.Date)
            throw new ValidationException("EndDate", "End date cannot be before the subscription start date.");

        if (request.EndDate.HasValue)
            subscription.EndDate = request.EndDate.Value;

        if (request.Notes != null)
            subscription.Notes = request.Notes;

        if (request.Discount.HasValue)
        {
            var revisedTotal = Math.Max(0m, subscription.Plan.Price - request.Discount.Value);
            if (revisedTotal < subscription.AmountPaid)
                throw new ValidationException("Discount", "The discount cannot reduce the subscription total below the amount already collected.");

            subscription.Discount = request.Discount.Value;
            subscription.TotalAmount = revisedTotal;
        }

        if (request.AmountPaid.HasValue)
        {
            if (request.AmountPaid.Value > subscription.TotalAmount)
                throw new ValidationException("AmountPaid", "Amount paid cannot exceed the subscription total");
            if (request.AmountPaid.Value < subscription.AmountPaid)
                throw new ConflictException("Paid amount cannot be reduced because the payment ledger is immutable. Record a refund instead.");
            var delta = request.AmountPaid.Value - subscription.AmountPaid;
            subscription.AmountPaid = request.AmountPaid.Value;
            SubscriptionPaymentLedger.Append(
                _context,
                tenantId,
                subscription,
                delta,
                subscription.PaymentMethod ?? PaymentMethod.Cash,
                Guid.TryParse(_currentUserService.UserId, out var receivedById) ? receivedById : null,
                DateTime.UtcNow,
                "Subscription payment adjustment");
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }
}

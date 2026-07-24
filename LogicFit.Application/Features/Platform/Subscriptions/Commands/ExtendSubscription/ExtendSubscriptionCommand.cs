using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Subscriptions.Commands.ExtendSubscription;

public sealed class ExtendSubscriptionCommand : IRequest<DateTime>
{
    public Guid SubscriptionId { get; init; }
    public int Days { get; init; }
}

public sealed class ExtendSubscriptionHandler(IApplicationDbContext context, IDateTimeService clock) : IRequestHandler<ExtendSubscriptionCommand, DateTime>
{
    public async Task<DateTime> Handle(ExtendSubscriptionCommand request, CancellationToken cancellationToken)
    {
        if (request.Days <= 0 || request.Days > 3660) throw new ValidationException("Days must be between 1 and 3660.");
        var subscription = await context.TenantSubscriptions.FirstOrDefaultAsync(x => x.Id == request.SubscriptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(TenantSubscription), request.SubscriptionId);
        if (subscription.Status == TenantSubscriptionStatus.Cancelled) throw new ConflictException("Cancelled subscriptions cannot be extended.");
        var now = clock.UtcNow;
        var from = subscription.EndDate.HasValue && subscription.EndDate.Value > now ? subscription.EndDate.Value : now;
        subscription.EndDate = from.AddDays(request.Days);
        subscription.RenewDate = subscription.EndDate;
        if (subscription.Status == TenantSubscriptionStatus.Expired || subscription.Status == TenantSubscriptionStatus.GracePeriod)
            subscription.Status = TenantSubscriptionStatus.Active;
        await context.SaveChangesAsync(cancellationToken);
        return subscription.EndDate.Value;
    }
}

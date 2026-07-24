using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Subscriptions.Commands.TransitionSubscription;

public sealed class TransitionSubscriptionCommand : IRequest<TenantSubscriptionStatus>
{
    public Guid SubscriptionId { get; init; }
    public TenantSubscriptionStatus TargetStatus { get; init; }
}

public sealed class TransitionSubscriptionHandler(IApplicationDbContext context) : IRequestHandler<TransitionSubscriptionCommand, TenantSubscriptionStatus>
{
    public async Task<TenantSubscriptionStatus> Handle(TransitionSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await context.TenantSubscriptions.FirstOrDefaultAsync(x => x.Id == request.SubscriptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(TenantSubscription), request.SubscriptionId);
        if (!TenantSubscriptionStateMachine.CanTransition(subscription.Status, request.TargetStatus))
            throw new ConflictException($"Subscription cannot transition from {subscription.Status} to {request.TargetStatus}.");
        subscription.Status = request.TargetStatus;
        await context.SaveChangesAsync(cancellationToken);
        return subscription.Status;
    }
}

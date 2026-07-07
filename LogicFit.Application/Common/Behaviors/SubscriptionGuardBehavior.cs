using LogicFit.Application.Common.Interfaces;
using MediatR;

namespace LogicFit.Application.Common.Behaviors;

/// <summary>
/// Before a command runs, enforces any plan feature-gate (<see cref="IRequireFeature"/>) and usage
/// quota (<see cref="IRequireQuota"/>) it declares. Throws SubscriptionLimitException on violation.
/// </summary>
public class SubscriptionGuardBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ITenantSubscriptionGuard _guard;

    public SubscriptionGuardBehavior(ITenantSubscriptionGuard guard)
    {
        _guard = guard;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IRequireFeature feature)
        {
            await _guard.EnsureFeatureAsync(feature.RequiredFeatureCode, cancellationToken);
        }

        if (request is IRequireQuota quota)
        {
            await _guard.EnsureQuotaAsync(quota.QuotaResource, cancellationToken);
        }

        return await next();
    }
}

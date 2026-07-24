using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Subscriptions.Queries.PreviewUpgrade;

public sealed class PreviewUpgradeQuery : IRequest<UpgradePreviewDto>
{
    public Guid SubscriptionId { get; init; }
    public Guid TargetPlanId { get; init; }
}
public sealed record UpgradePreviewDto(decimal Amount, string Currency, int RemainingDays, int CurrentDurationDays, Guid TargetPlanId);
public sealed class PreviewUpgradeHandler(IApplicationDbContext context, IDateTimeService clock) : IRequestHandler<PreviewUpgradeQuery, UpgradePreviewDto>
{
    public async Task<UpgradePreviewDto> Handle(PreviewUpgradeQuery request, CancellationToken cancellationToken)
    {
        var sub = await context.TenantSubscriptions.Include(x => x.Plan).FirstOrDefaultAsync(x => x.Id == request.SubscriptionId, cancellationToken) ?? throw new NotFoundException(nameof(TenantSubscription), request.SubscriptionId);
        var target = await context.Plans.FirstOrDefaultAsync(x => x.Id == request.TargetPlanId && x.IsActive, cancellationToken) ?? throw new NotFoundException(nameof(Plan), request.TargetPlanId);
        var remaining = Math.Max(0, (int)Math.Ceiling(((sub.EndDate ?? clock.UtcNow) - clock.UtcNow).TotalDays));
        var duration = sub.Plan?.DurationInDays ?? 30;
        return new UpgradePreviewDto(SubscriptionUpgradePricing.Calculate(sub.Amount, target.Price, remaining, duration), target.Currency, remaining, duration, target.Id);
    }
}

using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Features.Commands.SetTenantOverride;

public sealed class SetTenantOverrideCommand : IRequest<Guid>
{
    public Guid TenantId { get; init; }
    public Guid FeatureId { get; init; }
    public bool IsEnabled { get; init; }
    public int? LimitOverride { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime StartsAt { get; init; }
    public DateTime? EndsAt { get; init; }
}

public sealed class SetTenantOverrideCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<SetTenantOverrideCommand, Guid>
{
    public async Task<Guid> Handle(SetTenantOverrideCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new ArgumentException("Override reason is required.");
        if (request.EndsAt.HasValue && request.EndsAt.Value <= request.StartsAt) throw new ArgumentException("Override end must be after start.");
        var feature = await context.Features.FirstOrDefaultAsync(f => f.Id == request.FeatureId, cancellationToken)
            ?? throw new KeyNotFoundException("Feature not found.");
        var existing = await context.TenantFeatures.FirstOrDefaultAsync(x => x.TenantId == request.TenantId && x.FeatureId == request.FeatureId, cancellationToken);
        if (existing == null) { existing = new TenantFeature { TenantId = request.TenantId, FeatureId = feature.Id }; context.TenantFeatures.Add(existing); }
        existing.IsEnabled = request.IsEnabled;
        existing.LimitOverride = request.LimitOverride;
        existing.Reason = request.Reason.Trim();
        existing.StartsAt = request.StartsAt == default ? DateTime.UtcNow : request.StartsAt.ToUniversalTime();
        existing.EndsAt = request.EndsAt?.ToUniversalTime();
        existing.GrantedByUserId = Guid.TryParse(currentUser.UserId, out var id) ? id : null;
        await context.SaveChangesAsync(cancellationToken);
        return existing.Id;
    }
}

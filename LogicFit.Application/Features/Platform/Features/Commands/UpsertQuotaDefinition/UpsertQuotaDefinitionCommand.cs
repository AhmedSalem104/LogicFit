using LogicFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Features.Commands.UpsertQuotaDefinition;

public sealed class UpsertQuotaDefinitionCommand : IRequest<Guid>
{
    public Guid? Id { get; init; }
    public Guid FeatureId { get; init; }
    public string ResourceKey { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public int? DefaultLimit { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class UpsertQuotaDefinitionCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpsertQuotaDefinitionCommand, Guid>
{
    public async Task<Guid> Handle(UpsertQuotaDefinitionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ResourceKey) || string.IsNullOrWhiteSpace(request.Unit)) throw new ArgumentException("ResourceKey and Unit are required.");
        if (request.DefaultLimit < 0) throw new ArgumentException("DefaultLimit cannot be negative.");
        if (!await context.Features.AnyAsync(x => x.Id == request.FeatureId, cancellationToken)) throw new KeyNotFoundException("Feature not found.");
        var duplicate = await context.FeatureQuotaDefinitions.AnyAsync(x => x.FeatureId == request.FeatureId && x.ResourceKey == request.ResourceKey && x.Id != request.Id, cancellationToken);
        if (duplicate) throw new InvalidOperationException("Quota resource already exists for this feature.");
        var entity = request.Id.HasValue ? await context.FeatureQuotaDefinitions.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken) : null;
        if (entity == null) { entity = new Domain.Entities.FeatureQuotaDefinition { FeatureId = request.FeatureId }; context.FeatureQuotaDefinitions.Add(entity); }
        entity.ResourceKey = request.ResourceKey.Trim().ToLowerInvariant(); entity.Unit = request.Unit.Trim(); entity.DefaultLimit = request.DefaultLimit; entity.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken); return entity.Id;
    }
}

using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Features.Commands.SetFeatureDependency;

public sealed class SetFeatureDependencyCommand : IRequest<Guid>
{
    public Guid FeatureId { get; init; }
    public Guid DependsOnFeatureId { get; init; }
}

public sealed class SetFeatureDependencyHandler(IApplicationDbContext context) : IRequestHandler<SetFeatureDependencyCommand, Guid>
{
    public async Task<Guid> Handle(SetFeatureDependencyCommand request, CancellationToken cancellationToken)
    {
        if (request.FeatureId == request.DependsOnFeatureId) throw new ValidationException("A feature cannot depend on itself.");
        if (!await context.Features.AnyAsync(x => x.Id == request.FeatureId, cancellationToken) || !await context.Features.AnyAsync(x => x.Id == request.DependsOnFeatureId, cancellationToken))
            throw new NotFoundException(nameof(Feature), request.FeatureId);
        if (await context.FeatureDependencies.AnyAsync(x => x.FeatureId == request.FeatureId && x.DependsOnFeatureId == request.DependsOnFeatureId, cancellationToken))
            throw new ConflictException("Feature dependency already exists.");
        if (await context.FeatureDependencies.AnyAsync(x => x.FeatureId == request.DependsOnFeatureId && x.DependsOnFeatureId == request.FeatureId, cancellationToken))
            throw new ConflictException("Circular feature dependency is not allowed.");
        var edges = await context.FeatureDependencies.AsNoTracking().Select(x => new { x.FeatureId, x.DependsOnFeatureId }).ToListAsync(cancellationToken);
        var next = edges.Where(x => x.FeatureId == request.DependsOnFeatureId).Select(x => x.DependsOnFeatureId).ToHashSet();
        var visited = new HashSet<Guid>();
        while (next.Count > 0)
        {
            if (next.Contains(request.FeatureId)) throw new ConflictException("Circular feature dependency is not allowed.");
            var current = next.ToArray();
            next.Clear();
            foreach (var node in current)
                if (visited.Add(node)) foreach (var edge in edges.Where(x => x.FeatureId == node)) next.Add(edge.DependsOnFeatureId);
        }
        var entity = new FeatureDependency { FeatureId = request.FeatureId, DependsOnFeatureId = request.DependsOnFeatureId };
        context.FeatureDependencies.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

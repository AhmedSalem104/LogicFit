using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LogicFit.Application.Features.Platform.Features.Queries.GetFeatures;

namespace LogicFit.Application.Features.Platform.Features.Commands.UpsertFeature;

public sealed class UpsertFeatureCommand : IRequest<FeatureDto>
{
    public Guid? Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Module { get; init; }
    public bool IsFree { get; init; }
    public bool IsActive { get; init; } = true;
    public bool SupportsQuota { get; init; }
    public FeatureLifecycleStatus Status { get; init; } = FeatureLifecycleStatus.Active;
}

public sealed class UpsertFeatureCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpsertFeatureCommand, FeatureDto>
{
    public async Task<FeatureDto> Handle(UpsertFeatureCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(code) || !code.Contains('.'))
            throw new ArgumentException("Feature code must be a stable dotted key, e.g. members.manage.");

        var feature = request.Id.HasValue
            ? await context.Features.FirstOrDefaultAsync(f => f.Id == request.Id.Value, cancellationToken)
            : null;
        if (feature == null)
        {
            if (await context.Features.AnyAsync(f => f.Code == code, cancellationToken))
                throw new InvalidOperationException("Feature code already exists.");
            feature = new Feature { Code = code };
            context.Features.Add(feature);
        }
        else if (!string.Equals(feature.Code, code, StringComparison.OrdinalIgnoreCase) &&
                 await context.Features.AnyAsync(f => f.Code == code && f.Id != feature.Id, cancellationToken))
            throw new InvalidOperationException("Feature code already exists.");

        feature.Code = code;
        feature.Name = string.IsNullOrWhiteSpace(request.NameEn) ? request.Name : request.NameEn!;
        feature.NameAr = request.NameAr;
        feature.NameEn = request.NameEn;
        feature.Description = request.Description;
        feature.Module = request.Module;
        feature.IsFree = request.IsFree;
        feature.IsActive = request.IsActive && request.Status != FeatureLifecycleStatus.Archived;
        feature.SupportsQuota = request.SupportsQuota;
        feature.Status = request.Status;
        await context.SaveChangesAsync(cancellationToken);
        return new FeatureDto { Id = feature.Id, Code = feature.Code, Name = feature.Name, Description = feature.Description, IsActive = feature.IsActive };
    }
}

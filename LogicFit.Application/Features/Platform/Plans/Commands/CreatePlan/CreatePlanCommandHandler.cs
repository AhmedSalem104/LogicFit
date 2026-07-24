using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.Plans.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Plans.Commands.CreatePlan;

public class CreatePlanCommandHandler : IRequestHandler<CreatePlanCommand, PlanDto>
{
    private readonly IApplicationDbContext _context;

    public CreatePlanCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PlanDto> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        var nameTaken = await _context.Plans.AnyAsync(p => p.Name == request.Name, cancellationToken);
        if (nameTaken)
        {
            throw new ConflictException($"A plan named '{request.Name}' already exists");
        }

        var plan = new Plan
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Currency = request.Currency,
            BillingCycle = request.BillingCycle,
            DurationInDays = request.DurationInDays,
            MaxMembers = request.MaxMembers,
            MaxCoaches = request.MaxCoaches,
            MaxBranches = request.MaxBranches,
            MaxEmployees = request.MaxEmployees,
            MaxStorageMB = request.MaxStorageMB,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder
        };
        _context.Plans.Add(plan);

        await AttachFeaturesAsync(plan.Id, request.FeatureCodes, request.FeatureLimits, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new PlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            Currency = plan.Currency,
            BillingCycle = plan.BillingCycle,
            DurationInDays = plan.DurationInDays,
            MaxMembers = plan.MaxMembers,
            MaxCoaches = plan.MaxCoaches,
            MaxBranches = plan.MaxBranches,
            MaxEmployees = plan.MaxEmployees,
            MaxStorageMB = plan.MaxStorageMB,
            IsActive = plan.IsActive,
            DisplayOrder = plan.DisplayOrder,
            Features = request.FeatureCodes.Distinct().ToList()
            ,FeatureLimits = request.FeatureLimits
        };
    }

    private async Task AttachFeaturesAsync(Guid planId, List<string> featureCodes, Dictionary<string, int?> limits, CancellationToken cancellationToken)
    {
        if (featureCodes.Count == 0) return;

        var features = await _context.Features
            .Where(f => featureCodes.Contains(f.Code))
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        foreach (var featureId in features)
        {
            var code = await _context.Features.Where(f => f.Id == featureId).Select(f => f.Code).FirstAsync(cancellationToken);
            _context.PlanFeatures.Add(new PlanFeature { PlanId = planId, FeatureId = featureId, LimitValue = limits.TryGetValue(code, out var value) ? value : null });
        }
    }
}

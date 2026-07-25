using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.Plans.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Plans.Commands.UpdatePlan;

public class UpdatePlanCommandHandler : IRequestHandler<UpdatePlanCommand, PlanDto>
{
    private readonly IApplicationDbContext _context;

    public UpdatePlanCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PlanDto> Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _context.Plans
            .Include(p => p.PlanFeatures)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (plan == null)
        {
            throw new NotFoundException(nameof(Plan), request.Id);
        }

        var nameTaken = await _context.Plans
            .AnyAsync(p => p.Name == request.Name && p.Id != request.Id, cancellationToken);
        if (nameTaken)
        {
            throw new ConflictException($"A plan named '{request.Name}' already exists");
        }

        plan.Name = request.Name;
        plan.Description = request.Description;
        plan.Price = request.Price;
        plan.Currency = request.Currency;
        plan.BillingCycle = request.BillingCycle;
        plan.DurationInDays = request.DurationInDays;
        plan.MaxMembers = request.MaxMembers;
        plan.MaxCoaches = request.MaxCoaches;
        plan.MaxBranches = request.MaxBranches;
        plan.MaxEmployees = request.MaxEmployees;
        plan.MaxStorageMB = request.MaxStorageMB;
        plan.IsActive = request.IsActive;
        plan.DisplayOrder = request.DisplayOrder;

        // Sync features: remove those no longer present, add the new ones.
        var desiredFeatureIds = await _context.Features
            .Where(f => request.FeatureCodes.Contains(f.Code))
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        var toRemove = plan.PlanFeatures.Where(pf => !desiredFeatureIds.Contains(pf.FeatureId)).ToList();
        foreach (var pf in toRemove)
        {
            _context.PlanFeatures.Remove(pf);
        }

        var currentFeatureIds = plan.PlanFeatures.Select(pf => pf.FeatureId).ToHashSet();
        foreach (var featureId in desiredFeatureIds.Where(id => !currentFeatureIds.Contains(id)))
        {
            var code = await _context.Features.Where(f => f.Id == featureId).Select(f => f.Code).FirstAsync(cancellationToken);
            _context.PlanFeatures.Add(new PlanFeature { PlanId = plan.Id, FeatureId = featureId, LimitValue = request.FeatureLimits.TryGetValue(code, out var limit) ? limit : null });
        }

        foreach (var feature in plan.PlanFeatures)
        {
            var code = await _context.Features.Where(f => f.Id == feature.FeatureId).Select(f => f.Code).FirstAsync(cancellationToken);
            feature.LimitValue = request.FeatureLimits.TryGetValue(code, out var limit) ? limit : null;
        }

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
            Features = request.FeatureCodes.Distinct().ToList(),
            FeatureLimits = request.FeatureLimits
        };
    }
}

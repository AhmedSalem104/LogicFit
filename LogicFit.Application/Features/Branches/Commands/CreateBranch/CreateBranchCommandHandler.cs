using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Branches.Commands.CreateBranch;

public class CreateBranchCommandHandler : IRequestHandler<CreateBranchCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ITenantSubscriptionGuard _subscriptionGuard;

    public CreateBranchCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ITenantSubscriptionGuard subscriptionGuard)
    {
        _context = context;
        _tenantService = tenantService;
        _subscriptionGuard = subscriptionGuard;
    }

    public async Task<Guid> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // The first branch is always allowed (the numeric limit is enforced by the Branches quota).
        // The MultiBranch feature only gates creating a SECOND (or later) branch, so a plan can grant
        // e.g. MaxBranches=3 yet still require the MultiBranch feature to actually open more than one.
        var existingBranches = await _context.Branches
            .CountAsync(b => b.TenantId == tenantId, cancellationToken);
        if (existingBranches >= 1)
        {
            await _subscriptionGuard.EnsureFeatureAsync(FeatureCodes.MultiBranch, cancellationToken);
        }

        if (request.IsDefault)
        {
            var existingDefault = await _context.Branches
                .Where(b => b.TenantId == tenantId && b.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var b in existingDefault)
                b.IsDefault = false;
        }

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            Address = request.Address,
            City = request.City,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsActive = request.IsActive,
            IsDefault = request.IsDefault,
            Capacity = request.Capacity,
            OpenTime = request.OpenTime,
            CloseTime = request.CloseTime,
            ManagerId = request.ManagerId,
            LogoUrl = request.LogoUrl,
            CoverImageUrl = request.CoverImageUrl
        };

        _context.Branches.Add(branch);
        await _context.SaveChangesAsync(cancellationToken);

        return branch.Id;
    }
}

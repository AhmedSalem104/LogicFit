using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.Tenants.DTOs;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Tenants.Queries.GetPlatformTenants;

public class GetPlatformTenantsQueryHandler : IRequestHandler<GetPlatformTenantsQuery, List<PlatformTenantDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPlatformTenantsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PlatformTenantDto>> Handle(GetPlatformTenantsQuery request, CancellationToken cancellationToken)
    {
        // Platform reads across all tenants (CurrentTenantId is null), excluding the sentinel tenant.
        var query = _context.Tenants
            .Where(t => t.Id != PlatformConstants.PlatformTenantId);

        if (request.Status.HasValue)
        {
            query = query.Where(t => t.Status == request.Status.Value);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new PlatformTenantDto
            {
                Id = t.Id,
                Name = t.Name,
                Subdomain = t.Subdomain,
                Status = t.Status,
                Email = t.Email,
                PhoneNumber = t.PhoneNumber,
                // Platform queries run with CurrentTenantId == null, so the tenant query filter is
                // already bypassed; an explicit IgnoreQueryFilters here would not translate in a subquery.
                MembersCount = _context.Users
                    .Count(u => u.TenantId == t.Id && u.Role == UserRole.Client && !u.IsDeleted),
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}

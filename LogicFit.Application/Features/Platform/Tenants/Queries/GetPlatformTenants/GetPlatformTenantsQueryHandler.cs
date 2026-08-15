using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Models;
using LogicFit.Application.Features.Platform.Tenants.DTOs;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Tenants.Queries.GetPlatformTenants;

public class GetPlatformTenantsQueryHandler : IRequestHandler<GetPlatformTenantsQuery, PagedResult<PlatformTenantDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPlatformTenantsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<PlatformTenantDto>> Handle(GetPlatformTenantsQuery request, CancellationToken cancellationToken)
    {
        // Platform reads across all tenants (CurrentTenantId is null), excluding the sentinel tenant.
        var query = _context.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Id != PlatformConstants.PlatformTenantId);

        if (request.Status == TenantStatus.Deleted)
        {
            query = query.Where(t => t.IsDeleted || t.Status == TenantStatus.Deleted);
        }
        else if (request.Status.HasValue)
        {
            query = query.Where(t => !t.IsDeleted && t.Status == request.Status.Value);
        }
        else
        {
            query = query.Where(t => !t.IsDeleted);
        }

        var (page, pageSize) = PageRequest.Normalize(request.Page, request.PageSize);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new PlatformTenantDto
            {
                Id = t.Id,
                Name = t.Name,
                Subdomain = t.Subdomain,
                WorkspaceType = t.WorkspaceType,
                Status = t.Status,
                Email = t.Email,
                PhoneNumber = t.PhoneNumber,
                IsDeleted = t.IsDeleted,
                DeletedAt = t.DeletedAt,
                // Platform queries run with CurrentTenantId == null, so the tenant query filter is
                // already bypassed; an explicit IgnoreQueryFilters here would not translate in a subquery.
                MembersCount = _context.Users
                    .Count(u => u.TenantId == t.Id && u.Role == UserRole.Client && !u.IsDeleted),
                CreatedAt = t.CreatedAt
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<PlatformTenantDto>.Create(items, totalCount, page, pageSize);
    }
}

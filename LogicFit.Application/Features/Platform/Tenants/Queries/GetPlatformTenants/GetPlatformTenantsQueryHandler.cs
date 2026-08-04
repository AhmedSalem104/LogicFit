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
        var tenants = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // User rows are still a compatibility projection while existing workspaces are being
        // transferred. Do not put this DbSet inside the Platform query: it is served by the
        // legacy compatibility context and EF cannot translate roots from two DbContexts.
        var tenantIds = tenants.Select(t => t.Id).ToArray();
        var memberCounts = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => tenantIds.Contains(u.TenantId) && u.Role == UserRole.Client && !u.IsDeleted)
            .GroupBy(u => u.TenantId)
            .Select(group => new { TenantId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

        var items = tenants.Select(t => new PlatformTenantDto
        {
            Id = t.Id,
            Name = t.Name,
            Subdomain = t.Subdomain,
            Status = t.Status,
            Email = t.Email,
            PhoneNumber = t.PhoneNumber,
            IsDeleted = t.IsDeleted,
            DeletedAt = t.DeletedAt,
            MembersCount = memberCounts.GetValueOrDefault(t.Id),
            CreatedAt = t.CreatedAt
        }).ToList();

        return PagedResult<PlatformTenantDto>.Create(items, totalCount, page, pageSize);
    }
}

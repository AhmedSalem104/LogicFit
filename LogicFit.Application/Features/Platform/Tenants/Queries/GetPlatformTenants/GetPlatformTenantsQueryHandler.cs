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
        var tenantRows = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new PlatformTenantDto
            {
                Id = t.Id,
                Name = t.Name,
                Subdomain = t.Subdomain,
                Status = t.Status,
                Email = t.Email,
                PhoneNumber = t.PhoneNumber,
                IsDeleted = t.IsDeleted,
                DeletedAt = t.DeletedAt,
                CreatedAt = t.CreatedAt
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Keep the cross-tenant member count as a separate query. A correlated count against the
        // tenant-filtered Users set can fail translation on some production EF/SQL combinations,
        // turning a harmless list request into a 500. The platform view must explicitly bypass
        // tenant filters for both sides of this read.
        var tenantIds = tenantRows.Select(t => t.Id).ToArray();
        var memberCounts = tenantIds.Length == 0
            ? new Dictionary<Guid, int>()
            : await _context.Users
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(u => tenantIds.Contains(u.TenantId) && u.Role == UserRole.Client && !u.IsDeleted)
                .GroupBy(u => u.TenantId)
                .Select(group => new { TenantId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

        foreach (var tenant in tenantRows)
            tenant.MembersCount = memberCounts.GetValueOrDefault(tenant.Id);

        return PagedResult<PlatformTenantDto>.Create(tenantRows, totalCount, page, pageSize);
    }
}

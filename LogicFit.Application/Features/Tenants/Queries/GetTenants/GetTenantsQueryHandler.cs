using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Tenants.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Tenants.Queries.GetTenants;

public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, List<TenantDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTenantsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TenantDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Tenants
            .Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                Subdomain = t.Subdomain ?? string.Empty,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}

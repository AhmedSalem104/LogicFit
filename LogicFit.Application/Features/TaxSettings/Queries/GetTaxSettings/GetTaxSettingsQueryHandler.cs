using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.TaxSettings.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.TaxSettings.Queries.GetTaxSettings;

public class GetTaxSettingsQueryHandler : IRequestHandler<GetTaxSettingsQuery, List<TaxSettingDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetTaxSettingsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<TaxSettingDto>> Handle(GetTaxSettingsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.TaxSettings.Where(t => t.TenantId == tenantId).AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(t => t.IsActive == request.IsActive.Value);

        var taxes = await query.OrderByDescending(t => t.IsDefault).ThenBy(t => t.Name).ToListAsync(cancellationToken);

        return taxes.Select(t => new TaxSettingDto
        {
            Id = t.Id,
            TenantId = t.TenantId,
            Name = t.Name,
            Rate = t.Rate,
            IsDefault = t.IsDefault,
            IsActive = t.IsActive,
            Description = t.Description
        }).ToList();
    }
}

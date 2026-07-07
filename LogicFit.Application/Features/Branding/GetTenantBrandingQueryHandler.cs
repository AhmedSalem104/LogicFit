using LogicFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Branding;

public class GetTenantBrandingQueryHandler : IRequestHandler<GetTenantBrandingQuery, BrandingDto?>
{
    private readonly IApplicationDbContext _context;

    public GetTenantBrandingQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BrandingDto?> Handle(GetTenantBrandingQuery request, CancellationToken cancellationToken)
    {
        var id = request.Identifier.ToLowerInvariant();

        // Anonymous lookup: match by subdomain or custom domain (bypass tenant filter).
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => !t.IsDeleted && (t.Subdomain == id || t.CustomDomain == id), cancellationToken);

        if (tenant == null)
        {
            return null;
        }

        var b = tenant.BrandingSettings;
        return new BrandingDto
        {
            TenantId = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            AppName = b?.AppName,
            LogoUrl = b?.LogoUrl ?? tenant.LogoUrl,
            CoverImageUrl = tenant.CoverImageUrl,
            PrimaryColor = b?.PrimaryColor,
            SecondaryColor = b?.SecondaryColor,
            FontFamily = b?.FontFamily,
            CustomCss = b?.CustomCss,
            InvoiceLogoUrl = b?.InvoiceLogoUrl,
            SupportPhone = b?.SupportPhone,
            SupportEmail = b?.SupportEmail
        };
    }
}

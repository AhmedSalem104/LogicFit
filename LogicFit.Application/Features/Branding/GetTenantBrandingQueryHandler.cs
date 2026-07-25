using LogicFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
        var gallery = string.IsNullOrWhiteSpace(tenant.GalleryImagesJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(tenant.GalleryImagesJson) ?? new List<string>();
        var assets = await _context.TenantBrandAssets.AsNoTracking()
            .Where(a => a.TenantId == tenant.Id && a.IsActive)
            .OrderBy(a => a.SortOrder)
            .Take(5)
            .Select(a => new BrandAssetDto
            {
                Id = a.Id, AssetType = a.AssetType, ImageUrl = a.ImageUrl,
                ThumbnailUrl = a.ThumbnailUrl, DesktopImageUrl = a.DesktopImageUrl,
                TabletImageUrl = a.TabletImageUrl, MobileImageUrl = a.MobileImageUrl,
                Title = a.Title, AltText = a.AltText, FocalPointX = a.FocalPointX,
                FocalPointY = a.FocalPointY, CropPosition = a.CropPosition, SortOrder = a.SortOrder
            }).ToListAsync(cancellationToken);
        return new BrandingDto
        {
            TenantId = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            AppName = b?.AppName,
            LogoUrl = b?.LogoUrl ?? tenant.LogoUrl,
            LogoDarkUrl = b?.LogoDarkUrl,
            LogoLightUrl = b?.LogoLightUrl,
            LogoIconUrl = b?.LogoIconUrl,
            FaviconUrl = b?.FaviconUrl,
            CoverImageUrl = tenant.CoverImageUrl,
            LoginBackgroundUrl = b?.LoginBackgroundUrl,
            DashboardBannerUrl = b?.DashboardBannerUrl,
            GalleryImages = gallery.Take(5).ToList(),
            Assets = assets,
            PrimaryColor = b?.PrimaryColor,
            PrimaryHoverColor = b?.PrimaryHoverColor,
            PrimaryForegroundColor = b?.PrimaryForegroundColor,
            SecondaryColor = b?.SecondaryColor,
            SecondaryHoverColor = b?.SecondaryHoverColor,
            SecondaryForegroundColor = b?.SecondaryForegroundColor,
            AccentColor = b?.AccentColor,
            BackgroundColor = b?.BackgroundColor,
            SurfaceColor = b?.SurfaceColor,
            CardColor = b?.CardColor,
            SidebarColor = b?.SidebarColor,
            SidebarTextColor = b?.SidebarTextColor,
            HeaderColor = b?.HeaderColor,
            HeaderTextColor = b?.HeaderTextColor,
            TextPrimaryColor = b?.TextPrimaryColor,
            TextSecondaryColor = b?.TextSecondaryColor,
            BorderColor = b?.BorderColor,
            InputBackgroundColor = b?.InputBackgroundColor,
            SuccessColor = b?.SuccessColor,
            WarningColor = b?.WarningColor,
            DangerColor = b?.DangerColor,
            InfoColor = b?.InfoColor,
            BorderRadius = b?.BorderRadius,
            ThemeMode = b?.ThemeMode,
            FontFamily = b?.FontFamily,
            CustomCss = b?.CustomCss,
            InvoiceLogoUrl = b?.InvoiceLogoUrl,
            SupportPhone = b?.SupportPhone,
            SupportEmail = b?.SupportEmail
        };
    }
}

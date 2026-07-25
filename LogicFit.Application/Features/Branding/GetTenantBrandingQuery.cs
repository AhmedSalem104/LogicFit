using MediatR;

namespace LogicFit.Application.Features.Branding;

/// <summary>Public lookup of a gym's white-label branding by subdomain or custom domain (for theming pre-login).</summary>
public class GetTenantBrandingQuery : IRequest<BrandingDto?>
{
    public string Identifier { get; set; } = string.Empty;
}

public class BrandingDto
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Subdomain { get; set; }
    public string? AppName { get; set; }
    public string? LogoUrl { get; set; }
    public string? LogoDarkUrl { get; set; }
    public string? LogoLightUrl { get; set; }
    public string? LogoIconUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? LoginBackgroundUrl { get; set; }
    public string? DashboardBannerUrl { get; set; }
    public List<string> GalleryImages { get; set; } = new();
    public List<BrandAssetDto> Assets { get; set; } = new();
    public string? PrimaryColor { get; set; }
    public string? PrimaryHoverColor { get; set; }
    public string? PrimaryForegroundColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? SecondaryHoverColor { get; set; }
    public string? SecondaryForegroundColor { get; set; }
    public string? AccentColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? SurfaceColor { get; set; }
    public string? CardColor { get; set; }
    public string? SidebarColor { get; set; }
    public string? SidebarTextColor { get; set; }
    public string? HeaderColor { get; set; }
    public string? HeaderTextColor { get; set; }
    public string? TextPrimaryColor { get; set; }
    public string? TextSecondaryColor { get; set; }
    public string? BorderColor { get; set; }
    public string? InputBackgroundColor { get; set; }
    public string? SuccessColor { get; set; }
    public string? WarningColor { get; set; }
    public string? DangerColor { get; set; }
    public string? InfoColor { get; set; }
    public string? BorderRadius { get; set; }
    public string? ThemeMode { get; set; }
    public string? FontFamily { get; set; }
    public string? CustomCss { get; set; }
    public string? InvoiceLogoUrl { get; set; }
    public string? SupportPhone { get; set; }
    public string? SupportEmail { get; set; }
}

public class BrandAssetDto
{
    public Guid Id { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? DesktopImageUrl { get; set; }
    public string? TabletImageUrl { get; set; }
    public string? MobileImageUrl { get; set; }
    public string? Title { get; set; }
    public string? AltText { get; set; }
    public decimal FocalPointX { get; set; }
    public decimal FocalPointY { get; set; }
    public string? CropPosition { get; set; }
    public int SortOrder { get; set; }
}

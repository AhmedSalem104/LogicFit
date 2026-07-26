using MediatR;

namespace LogicFit.Application.Features.GymProfile.Commands.UpdateGymProfile;

public class UpdateGymProfileCommand : IRequest<bool>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public List<string>? GalleryImages { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? LogoDarkUrl { get; set; }
    public string? LogoLightUrl { get; set; }
    public string? LogoIconUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? LoginBackgroundUrl { get; set; }
    public string? DashboardBannerUrl { get; set; }
    public string? PrimaryHoverColor { get; set; }
    public string? PrimaryForegroundColor { get; set; }
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

    // White-label
    public string? AppName { get; set; }
    public string? FontFamily { get; set; }
    public string? CustomCss { get; set; }
    public string? InvoiceLogoUrl { get; set; }
    public string? SupportPhone { get; set; }
    public string? SupportEmail { get; set; }
    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? OpeningHours { get; set; }
    public string? CustomDomain { get; set; }
}

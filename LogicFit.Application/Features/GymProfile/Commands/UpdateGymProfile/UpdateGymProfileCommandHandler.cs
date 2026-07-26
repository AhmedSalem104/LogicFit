using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.GymProfile.Commands.UpdateGymProfile;

public class UpdateGymProfileCommandHandler : IRequestHandler<UpdateGymProfileCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ITenantSubscriptionGuard _subscriptionGuard;

    public UpdateGymProfileCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ITenantSubscriptionGuard subscriptionGuard)
    {
        _context = context;
        _tenantService = tenantService;
        _subscriptionGuard = subscriptionGuard;
    }

    public async Task<bool> Handle(UpdateGymProfileCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Basic profile edits (name, colors, logo) are available to everyone. The distinctive
        // white-label fields (app name, custom fonts/CSS, invoice logo, support branding) require the
        // WhiteLabel feature; pointing the gym at a custom domain requires the CustomDomain feature.
        // Only enforce when those specific fields are actually being changed.
        // Empty values are emitted by the settings form for untouched optional
        // fields. They must not turn a color/profile update into a paid
        // WhiteLabel operation. Non-empty branding changes still require the
        // WhiteLabel feature; clearing an existing value remains allowed.
        var setsWhiteLabel =
            !string.IsNullOrWhiteSpace(request.AppName) || !string.IsNullOrWhiteSpace(request.FontFamily) ||
            !string.IsNullOrWhiteSpace(request.CustomCss) || !string.IsNullOrWhiteSpace(request.InvoiceLogoUrl) ||
            !string.IsNullOrWhiteSpace(request.SupportPhone) || !string.IsNullOrWhiteSpace(request.SupportEmail);
        if (setsWhiteLabel)
        {
            await _subscriptionGuard.EnsureFeatureAsync(FeatureCodes.WhiteLabel, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.CustomDomain))
        {
            await _subscriptionGuard.EnsureFeatureAsync(FeatureCodes.CustomDomain, cancellationToken);
        }

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant == null)
            throw new NotFoundException("Gym", tenantId);

        // Update fields if provided
        if (!string.IsNullOrEmpty(request.Name))
            tenant.Name = request.Name;

        if (request.Description != null)
            tenant.Description = request.Description;

        if (request.Address != null)
            tenant.Address = request.Address;

        if (request.PhoneNumber != null)
            tenant.PhoneNumber = request.PhoneNumber;

        if (request.Email != null)
            tenant.Email = request.Email;

        if (request.LogoUrl != null)
            tenant.LogoUrl = request.LogoUrl;

        if (request.CoverImageUrl != null)
            tenant.CoverImageUrl = request.CoverImageUrl;

        if (request.GalleryImages != null)
            tenant.GalleryImagesJson = JsonSerializer.Serialize(request.GalleryImages);

        if (request.CustomDomain != null)
            tenant.CustomDomain = string.IsNullOrWhiteSpace(request.CustomDomain) ? null : request.CustomDomain.ToLowerInvariant();

        // Update branding / white-label settings
        var branding = new[]
        {
            request.PrimaryColor, request.SecondaryColor, request.AppName, request.FontFamily,
            request.CustomCss, request.InvoiceLogoUrl, request.SupportPhone, request.SupportEmail,
            request.LogoDarkUrl, request.LogoLightUrl, request.LogoIconUrl, request.FaviconUrl,
            request.LoginBackgroundUrl, request.DashboardBannerUrl, request.PrimaryHoverColor,
            request.PrimaryForegroundColor, request.SecondaryHoverColor, request.SecondaryForegroundColor,
            request.AccentColor, request.BackgroundColor, request.SurfaceColor, request.CardColor,
            request.SidebarColor, request.SidebarTextColor, request.HeaderColor, request.HeaderTextColor,
            request.TextPrimaryColor, request.TextSecondaryColor, request.BorderColor, request.InputBackgroundColor,
            request.SuccessColor, request.WarningColor, request.DangerColor, request.InfoColor,
            request.BorderRadius, request.ThemeMode
        };
        if (branding.Any(v => v != null) || request.LogoUrl != null)
        {
            tenant.BrandingSettings ??= new BrandingSettings();

            if (request.PrimaryColor != null) tenant.BrandingSettings.PrimaryColor = request.PrimaryColor;
            if (request.SecondaryColor != null) tenant.BrandingSettings.SecondaryColor = request.SecondaryColor;
            if (request.LogoUrl != null) tenant.BrandingSettings.LogoUrl = request.LogoUrl;
            if (request.LogoDarkUrl != null) tenant.BrandingSettings.LogoDarkUrl = request.LogoDarkUrl;
            if (request.LogoLightUrl != null) tenant.BrandingSettings.LogoLightUrl = request.LogoLightUrl;
            if (request.LogoIconUrl != null) tenant.BrandingSettings.LogoIconUrl = request.LogoIconUrl;
            if (request.FaviconUrl != null) tenant.BrandingSettings.FaviconUrl = request.FaviconUrl;
            if (request.LoginBackgroundUrl != null) tenant.BrandingSettings.LoginBackgroundUrl = request.LoginBackgroundUrl;
            if (request.DashboardBannerUrl != null) tenant.BrandingSettings.DashboardBannerUrl = request.DashboardBannerUrl;
            if (request.PrimaryHoverColor != null) tenant.BrandingSettings.PrimaryHoverColor = request.PrimaryHoverColor;
            if (request.PrimaryForegroundColor != null) tenant.BrandingSettings.PrimaryForegroundColor = request.PrimaryForegroundColor;
            if (request.SecondaryHoverColor != null) tenant.BrandingSettings.SecondaryHoverColor = request.SecondaryHoverColor;
            if (request.SecondaryForegroundColor != null) tenant.BrandingSettings.SecondaryForegroundColor = request.SecondaryForegroundColor;
            if (request.AccentColor != null) tenant.BrandingSettings.AccentColor = request.AccentColor;
            if (request.BackgroundColor != null) tenant.BrandingSettings.BackgroundColor = request.BackgroundColor;
            if (request.SurfaceColor != null) tenant.BrandingSettings.SurfaceColor = request.SurfaceColor;
            if (request.CardColor != null) tenant.BrandingSettings.CardColor = request.CardColor;
            if (request.SidebarColor != null) tenant.BrandingSettings.SidebarColor = request.SidebarColor;
            if (request.SidebarTextColor != null) tenant.BrandingSettings.SidebarTextColor = request.SidebarTextColor;
            if (request.HeaderColor != null) tenant.BrandingSettings.HeaderColor = request.HeaderColor;
            if (request.HeaderTextColor != null) tenant.BrandingSettings.HeaderTextColor = request.HeaderTextColor;
            if (request.TextPrimaryColor != null) tenant.BrandingSettings.TextPrimaryColor = request.TextPrimaryColor;
            if (request.TextSecondaryColor != null) tenant.BrandingSettings.TextSecondaryColor = request.TextSecondaryColor;
            if (request.BorderColor != null) tenant.BrandingSettings.BorderColor = request.BorderColor;
            if (request.InputBackgroundColor != null) tenant.BrandingSettings.InputBackgroundColor = request.InputBackgroundColor;
            if (request.SuccessColor != null) tenant.BrandingSettings.SuccessColor = request.SuccessColor;
            if (request.WarningColor != null) tenant.BrandingSettings.WarningColor = request.WarningColor;
            if (request.DangerColor != null) tenant.BrandingSettings.DangerColor = request.DangerColor;
            if (request.InfoColor != null) tenant.BrandingSettings.InfoColor = request.InfoColor;
            if (request.BorderRadius != null) tenant.BrandingSettings.BorderRadius = request.BorderRadius;
            if (request.ThemeMode != null) tenant.BrandingSettings.ThemeMode = request.ThemeMode;
            if (request.AppName != null) tenant.BrandingSettings.AppName = request.AppName;
            if (request.FontFamily != null) tenant.BrandingSettings.FontFamily = request.FontFamily;
            if (request.CustomCss != null) tenant.BrandingSettings.CustomCss = request.CustomCss;
            if (request.InvoiceLogoUrl != null) tenant.BrandingSettings.InvoiceLogoUrl = request.InvoiceLogoUrl;
            if (request.SupportPhone != null) tenant.BrandingSettings.SupportPhone = request.SupportPhone;
            if (request.SupportEmail != null) tenant.BrandingSettings.SupportEmail = request.SupportEmail;

            // BrandingSettings is stored through an EF value converter. EF can
            // miss in-place mutations of a mutable converted object, so replace
            // it with a detached copy to force a reliable column update.
            tenant.BrandingSettings = JsonSerializer.Deserialize<BrandingSettings>(
                JsonSerializer.Serialize(tenant.BrandingSettings));
        }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

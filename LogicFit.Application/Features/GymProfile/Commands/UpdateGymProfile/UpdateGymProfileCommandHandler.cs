using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.GymProfile.Commands.UpdateGymProfile;

public class UpdateGymProfileCommandHandler : IRequestHandler<UpdateGymProfileCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateGymProfileCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(UpdateGymProfileCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

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
            request.CustomCss, request.InvoiceLogoUrl, request.SupportPhone, request.SupportEmail
        };
        if (branding.Any(v => v != null) || request.LogoUrl != null)
        {
            tenant.BrandingSettings ??= new BrandingSettings();

            if (request.PrimaryColor != null) tenant.BrandingSettings.PrimaryColor = request.PrimaryColor;
            if (request.SecondaryColor != null) tenant.BrandingSettings.SecondaryColor = request.SecondaryColor;
            if (request.LogoUrl != null) tenant.BrandingSettings.LogoUrl = request.LogoUrl;
            if (request.AppName != null) tenant.BrandingSettings.AppName = request.AppName;
            if (request.FontFamily != null) tenant.BrandingSettings.FontFamily = request.FontFamily;
            if (request.CustomCss != null) tenant.BrandingSettings.CustomCss = request.CustomCss;
            if (request.InvoiceLogoUrl != null) tenant.BrandingSettings.InvoiceLogoUrl = request.InvoiceLogoUrl;
            if (request.SupportPhone != null) tenant.BrandingSettings.SupportPhone = request.SupportPhone;
            if (request.SupportEmail != null) tenant.BrandingSettings.SupportEmail = request.SupportEmail;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

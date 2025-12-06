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

        // Update branding settings
        if (request.PrimaryColor != null || request.SecondaryColor != null)
        {
            tenant.BrandingSettings ??= new BrandingSettings();

            if (request.PrimaryColor != null)
                tenant.BrandingSettings.PrimaryColor = request.PrimaryColor;

            if (request.SecondaryColor != null)
                tenant.BrandingSettings.SecondaryColor = request.SecondaryColor;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.GymProfile.DTOs;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.GymProfile.Queries.GetGymProfile;

public class GetGymProfileQueryHandler : IRequestHandler<GetGymProfileQuery, GymProfileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetGymProfileQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<GymProfileDto> Handle(GetGymProfileQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant == null)
            throw new NotFoundException("Gym", tenantId);

        // Get statistics
        var totalClients = await _context.Users
            .CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Client && !u.IsDeleted, cancellationToken);

        var activeClients = await _context.Users
            .CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Client && u.IsActive && !u.IsDeleted, cancellationToken);

        var totalCoaches = await _context.Users
            .CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Coach && !u.IsDeleted, cancellationToken);

        var totalSubscriptionPlans = await _context.SubscriptionPlans
            .CountAsync(sp => sp.TenantId == tenantId && !sp.IsDeleted, cancellationToken);

        var activeSubscriptions = await _context.ClientSubscriptions
            .CountAsync(cs => cs.TenantId == tenantId && cs.Status == SubscriptionStatus.Active && !cs.IsDeleted, cancellationToken);

        // Parse gallery images
        var galleryImages = new List<string>();
        if (!string.IsNullOrEmpty(tenant.GalleryImagesJson))
        {
            try
            {
                galleryImages = JsonSerializer.Deserialize<List<string>>(tenant.GalleryImagesJson) ?? new();
            }
            catch { }
        }

        return new GymProfileDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            Description = tenant.Description,
            Address = tenant.Address,
            PhoneNumber = tenant.PhoneNumber,
            Email = tenant.Email,
            LogoUrl = tenant.LogoUrl,
            CoverImageUrl = tenant.CoverImageUrl,
            GalleryImages = galleryImages,
            Status = tenant.Status.ToString(),
            BrandingSettings = tenant.BrandingSettings != null ? new BrandingSettingsDto
            {
                PrimaryColor = tenant.BrandingSettings.PrimaryColor,
                SecondaryColor = tenant.BrandingSettings.SecondaryColor,
                LogoUrl = tenant.BrandingSettings.LogoUrl
            } : null,
            Statistics = new GymStatisticsDto
            {
                TotalClients = totalClients,
                ActiveClients = activeClients,
                TotalCoaches = totalCoaches,
                TotalSubscriptionPlans = totalSubscriptionPlans,
                ActiveSubscriptions = activeSubscriptions
            }
        };
    }
}

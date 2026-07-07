using LogicFit.Application.Features.Platform.Tenants.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Platform.Tenants.Commands.CreateTenantWithOwner;

/// <summary>Platform-only onboarding: creates a gym (tenant) together with its Owner account.</summary>
public class CreateTenantWithOwnerCommand : IRequest<PlatformTenantDto>
{
    // Tenant
    public string Name { get; set; } = string.Empty;
    public string? Subdomain { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    // Owner account
    public string OwnerEmail { get; set; } = string.Empty;
    public string? OwnerPhoneNumber { get; set; }
    public string OwnerPassword { get; set; } = string.Empty;
    public string OwnerFullName { get; set; } = string.Empty;
}

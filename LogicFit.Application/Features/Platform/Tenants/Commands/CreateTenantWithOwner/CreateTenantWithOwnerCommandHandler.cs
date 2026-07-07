using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.Tenants.DTOs;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Tenants.Commands.CreateTenantWithOwner;

public class CreateTenantWithOwnerCommandHandler : IRequestHandler<CreateTenantWithOwnerCommand, PlatformTenantDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IRbacService _rbacService;

    public CreateTenantWithOwnerCommandHandler(IApplicationDbContext context, IRbacService rbacService)
    {
        _context = context;
        _rbacService = rbacService;
    }

    public async Task<PlatformTenantDto> Handle(CreateTenantWithOwnerCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Subdomain))
        {
            var subdomain = request.Subdomain.ToLowerInvariant();
            var subdomainTaken = await _context.Tenants
                .IgnoreQueryFilters()
                .AnyAsync(t => t.Subdomain == subdomain, cancellationToken);
            if (subdomainTaken)
            {
                throw new ConflictException($"Subdomain '{subdomain}' is already taken");
            }
        }

        var tenant = new Tenant
        {
            Name = request.Name,
            Subdomain = request.Subdomain?.ToLowerInvariant(),
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Status = TenantStatus.PendingApproval,
            BrandingSettings = new BrandingSettings
            {
                PrimaryColor = "#3B82F6",
                SecondaryColor = "#1E40AF"
            }
        };
        _context.Tenants.Add(tenant);

        var owner = new User
        {
            TenantId = tenant.Id,
            Email = request.OwnerEmail,
            PhoneNumber = request.OwnerPhoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.OwnerPassword),
            Role = UserRole.Owner,
            IsActive = true
        };
        _context.Users.Add(owner);

        if (!string.IsNullOrWhiteSpace(request.OwnerFullName))
        {
            _context.UserProfiles.Add(new UserProfile { UserId = owner.Id, FullName = request.OwnerFullName });
        }

        await _rbacService.EnsureUserInRoleAsync(owner.Id, tenant.Id, SystemRoles.Owner, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new PlatformTenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            Status = tenant.Status,
            Email = tenant.Email,
            PhoneNumber = tenant.PhoneNumber,
            CreatedAt = tenant.CreatedAt
        };
    }
}

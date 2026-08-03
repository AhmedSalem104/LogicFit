using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity;
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
    private readonly IDateTimeService _dateTimeService;

    public CreateTenantWithOwnerCommandHandler(
        IApplicationDbContext context,
        IRbacService rbacService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _rbacService = rbacService;
        _dateTimeService = dateTimeService;
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

        var normalizedOwnerEmail = IdentityEmailAddress.Normalize(request.OwnerEmail);
        var ownerIdentityExists = await _context.IdentityAccounts
            .IgnoreQueryFilters()
            .AnyAsync(x => x.NormalizedEmail == normalizedOwnerEmail, cancellationToken);
        if (ownerIdentityExists)
        {
            throw new ConflictException("The owner email is already registered. Use another email or the workspace invitation flow.");
        }

        var ownerPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.OwnerPassword);
        var ownerIdentity = new IdentityAccount
        {
            FullName = request.OwnerFullName.Trim(),
            Email = request.OwnerEmail.Trim(),
            NormalizedEmail = normalizedOwnerEmail,
            PhoneNumber = request.OwnerPhoneNumber,
            PasswordHash = ownerPasswordHash,
            // This identity was provisioned by an authenticated platform operator.
            EmailVerifiedAt = _dateTimeService.UtcNow
        };
        _context.IdentityAccounts.Add(ownerIdentity);

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
            IdentityAccountId = ownerIdentity.Id,
            TenantId = tenant.Id,
            Email = request.OwnerEmail,
            PhoneNumber = request.OwnerPhoneNumber,
            PasswordHash = ownerPasswordHash,
            Role = UserRole.Owner,
            IsActive = true
        };
        _context.Users.Add(owner);

        _context.WorkspaceMemberships.Add(new WorkspaceMembership
        {
            IdentityAccountId = ownerIdentity.Id,
            TenantId = tenant.Id,
            UserId = owner.Id,
            Role = UserRole.Owner,
            Status = WorkspaceMembershipStatus.PendingPlatformApproval
        });

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

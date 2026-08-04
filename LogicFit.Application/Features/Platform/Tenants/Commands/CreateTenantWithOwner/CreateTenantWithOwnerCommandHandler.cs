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

        var ownerIdentity = await _context.IdentityAccounts
            .SingleOrDefaultAsync(x => x.NormalizedEmail == IdentityEmailAddress.Normalize(request.OwnerEmail), cancellationToken);
        if (ownerIdentity is not null && !ownerIdentity.IsActive)
            throw new ConflictException("The owner Global Identity is inactive.");

        if (ownerIdentity is null)
        {
            ownerIdentity = new IdentityAccount
            {
                FullName = request.OwnerFullName,
                Email = request.OwnerEmail.Trim(),
                NormalizedEmail = IdentityEmailAddress.Normalize(request.OwnerEmail),
                PhoneNumber = request.OwnerPhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.OwnerPassword),
                IsActive = true,
                // This is an explicit Platform-admin provisioned account. The owner can use the
                // normal identity-first login immediately after the gym is approved; later resets
                // still use the one-time email-link flow.
                EmailVerifiedAt = _dateTimeService.UtcNow
            };
            _context.IdentityAccounts.Add(ownerIdentity);
        }

        var owner = new User
        {
            TenantId = tenant.Id,
            IdentityAccountId = ownerIdentity.Id,
            Email = request.OwnerEmail,
            PhoneNumber = request.OwnerPhoneNumber,
            PasswordHash = ownerIdentity.PasswordHash,
            Role = UserRole.Owner,
            IsActive = true
        };
        _context.Users.Add(owner);

        if (!string.IsNullOrWhiteSpace(request.OwnerFullName))
        {
            _context.UserProfiles.Add(new UserProfile { UserId = owner.Id, FullName = request.OwnerFullName });
        }

        await _rbacService.EnsureUserInRoleAsync(owner.Id, tenant.Id, SystemRoles.Owner, cancellationToken);

        _context.WorkspaceMemberships.Add(new WorkspaceMembership
        {
            TenantId = tenant.Id,
            IdentityAccountId = ownerIdentity.Id,
            UserId = owner.Id,
            Role = UserRole.Owner,
            Status = WorkspaceMembershipStatus.PendingPlatformApproval
        });

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

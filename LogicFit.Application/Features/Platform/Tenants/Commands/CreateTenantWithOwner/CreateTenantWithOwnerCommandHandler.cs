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
    private readonly IWorkspaceProvisioningSaga _provisioningSaga;

    public CreateTenantWithOwnerCommandHandler(
        IApplicationDbContext context,
        IRbacService rbacService,
        IDateTimeService dateTimeService,
        IWorkspaceProvisioningSaga provisioningSaga)
    {
        _context = context;
        _rbacService = rbacService;
        _dateTimeService = dateTimeService;
        _provisioningSaga = provisioningSaga;
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
        var ownerIdentity = await _context.IdentityAccounts
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedOwnerEmail, cancellationToken);
        if (ownerIdentity is not null && !ownerIdentity.IsActive)
            throw new ConflictException("The owner Global Identity is inactive.");

        if (ownerIdentity is null)
        {
            ownerIdentity = new IdentityAccount
            {
                FullName = request.OwnerFullName.Trim(),
                Email = request.OwnerEmail.Trim(),
                NormalizedEmail = normalizedOwnerEmail,
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

        var tenant = new Tenant
        {
            Name = request.Name,
            Subdomain = request.Subdomain?.ToLowerInvariant(),
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            WorkspaceType = WorkspaceType.Gym,
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
            PasswordHash = ownerIdentity.PasswordHash,
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

        // A platform-created gym is an already-approved workspace application. Keeping the
        // provisioning intent in ApplicationRequests lets the same retryable saga allocate one
        // resource, migrate it, seed the owner and record the mapping.
        var now = _dateTimeService.UtcNow;
        var provisioningApplication = new ApplicationRequest
        {
            IdentityAccountId = ownerIdentity.Id,
            ApplicationType = ApplicationType.GymWorkspaceCreation,
            Status = ApplicationRequestStatus.Approved,
            ProvisionedWorkspaceId = tenant.Id,
            TargetScopeKey = $"platform-gym:{tenant.Id:N}",
            RequestedRole = UserRole.Owner,
            PayloadJson = "{}",
            SubmittedAt = now,
            ReviewedAt = now,
            ReviewedBy = "platform-create"
        };
        _context.ApplicationRequests.Add(provisioningApplication);
        _context.ApplicationRequestRevisions.Add(new ApplicationRequestRevision
        {
            ApplicationRequestId = provisioningApplication.Id,
            RevisionNumber = 1,
            PayloadJson = provisioningApplication.PayloadJson,
            SubmittedAt = now,
            SubmittedBy = "platform-create"
        });

        await _context.SaveChangesAsync(cancellationToken);
        var provisioning = await _provisioningSaga.RunAsync(provisioningApplication.Id, cancellationToken);
        if (provisioning.Status == ProvisioningJobStatus.AwaitingDatabaseCapacity)
            throw new ConflictException("لا توجد قاعدة بيانات متاحة أضف Connection جديدا أولا.");

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

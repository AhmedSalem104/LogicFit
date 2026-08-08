using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
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
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkspaceProvisioningSaga _provisioningSaga;

    public CreateTenantWithOwnerCommandHandler(
        IApplicationDbContext context,
        IRbacService rbacService,
        IDateTimeService dateTimeService,
        ICurrentUserService currentUserService,
        IWorkspaceProvisioningSaga provisioningSaga)
    {
        _context = context;
        _rbacService = rbacService;
        _dateTimeService = dateTimeService;
        _currentUserService = currentUserService;
        _provisioningSaga = provisioningSaga;
    }

    public async Task<PlatformTenantDto> Handle(CreateTenantWithOwnerCommand request, CancellationToken cancellationToken)
    {
        var normalizedOwnerEmail = IdentityEmailAddress.Normalize(request.OwnerEmail);
        var scopeKey = PlatformGymIdempotency.BuildScopeKey(request, _currentUserService.UserId);
        var existingApplication = await FindExistingApplicationAsync(scopeKey, cancellationToken);
        if (existingApplication is not null)
            return await ResumeExistingRequestAsync(existingApplication, request, cancellationToken);

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
                PhoneNumber = request.OwnerPhoneNumber?.Trim(),
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
            Name = request.Name.Trim(),
            Subdomain = request.Subdomain?.ToLowerInvariant(),
            Email = request.Email?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
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
            Email = request.OwnerEmail.Trim(),
            PhoneNumber = request.OwnerPhoneNumber?.Trim(),
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
            _context.UserProfiles.Add(new UserProfile { UserId = owner.Id, FullName = request.OwnerFullName.Trim() });
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
            TargetScopeKey = scopeKey,
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

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException exception)
        {
            // The filtered scope index is the final guard against two concurrent requests. Do
            // not expose provider/SQL details; the caller can safely retry the same request and
            // the committed application will then be resumed by its scope key.
            throw new ProvisioningException(
                ProvisioningErrorCodes.GymProvisioningFailed,
                503,
                "Gym registration could not be committed. Retry the same request; no new database mapping was created by this attempt.",
                retryable: true,
                innerException: exception);
        }
        var provisioning = await RunProvisioningSafelyAsync(
            provisioningApplication.Id,
            tenant.Id,
            cancellationToken);
        ProvisioningOutcomeGuard.EnsureCompleted(provisioning);

        return ToDto(tenant);
    }

    private async Task<PlatformTenantDto> ResumeExistingRequestAsync(
        ApplicationRequest application,
        CreateTenantWithOwnerCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = application.ProvisionedWorkspaceId
            ?? throw new ProvisioningException(
                ProvisioningErrorCodes.GymProvisioningFailed,
                503,
                "The previous gym provisioning request has no workspace reference. An operator must review it before retrying.",
                retryable: false,
                applicationRequestId: application.Id);
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken)
            ?? throw new ProvisioningException(
                ProvisioningErrorCodes.GymProvisioningFailed,
                503,
                "The previous gym provisioning request has no workspace record. An operator must review it before retrying.",
                retryable: false,
                tenantId: tenantId,
                applicationRequestId: application.Id);

        if (!PlatformGymIdempotency.MatchesRequest(request, tenant, application.IdentityAccount))
        {
            throw new ProvisioningException(
                ProvisioningErrorCodes.IdempotencyKeyReused,
                409,
                "This idempotency key was already used for a different gym request.",
                retryable: false,
                tenantId: tenantId,
                applicationRequestId: application.Id);
        }

        var job = await _context.ProvisioningJobs
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.ApplicationRequestId == application.Id, cancellationToken);
        if (job?.Status == ProvisioningJobStatus.Provisioning)
        {
            throw new ProvisioningException(
                ProvisioningErrorCodes.ProvisioningInProgress,
                409,
                "Gym provisioning is still running. Retry after the current attempt completes.",
                retryable: true,
                tenantId: tenantId,
                applicationRequestId: application.Id,
                databaseResourceId: job.DatabaseResourceId);
        }

        var provisioning = await RunProvisioningSafelyAsync(application.Id, tenantId, cancellationToken);
        ProvisioningOutcomeGuard.EnsureCompleted(provisioning);
        return ToDto(tenant);
    }

    private async Task<ApplicationRequest?> FindExistingApplicationAsync(
        string scopeKey,
        CancellationToken cancellationToken)
        => await _context.ApplicationRequests
            .IgnoreQueryFilters()
            .Include(x => x.IdentityAccount)
            .Where(x => x.ApplicationType == ApplicationType.GymWorkspaceCreation &&
                        x.TargetScopeKey == scopeKey &&
                        x.ProvisionedWorkspaceId.HasValue)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<WorkspaceProvisioningOutcome> RunProvisioningSafelyAsync(
        Guid applicationRequestId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _provisioningSaga.RunAsync(applicationRequestId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ProvisioningException(
                ProvisioningErrorCodes.GymProvisioningFailed,
                503,
                "Gym provisioning did not complete. An operator can repair the database resource and retry the provisioning job.",
                retryable: true,
                tenantId: tenantId,
                applicationRequestId: applicationRequestId,
                innerException: exception);
        }
    }

    private static PlatformTenantDto ToDto(Tenant tenant)
        => new()
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            Status = tenant.Status,
            Email = tenant.Email,
            PhoneNumber = tenant.PhoneNumber,
            CreatedAt = tenant.CreatedAt,
            IsDeleted = tenant.IsDeleted,
            DeletedAt = tenant.DeletedAt
        };
}

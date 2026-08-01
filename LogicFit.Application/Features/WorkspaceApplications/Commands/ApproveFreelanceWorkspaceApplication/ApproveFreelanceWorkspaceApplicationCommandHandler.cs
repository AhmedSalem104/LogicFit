using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.Services;
using LogicFit.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.ApproveFreelanceWorkspaceApplication;

/// <summary>
/// Idempotent, two-stage provisioning. The first save reserves exactly one Tenant in Provisioning;
/// a retry reuses it, and a second successful save makes it Active together with its owner/membership.
/// </summary>
public sealed class ApproveFreelanceWorkspaceApplicationCommandHandler
    : IRequestHandler<ApproveFreelanceWorkspaceApplicationCommand, PlatformApplicationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public ApproveFreelanceWorkspaceApplicationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<PlatformApplicationDto> Handle(
        ApproveFreelanceWorkspaceApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _context.ApplicationRequests
            .Include(x => x.IdentityAccount)
            .FirstOrDefaultAsync(x => x.Id == request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApplicationRequest), request.ApplicationId);

        if (application.ApplicationType != ApplicationType.FreelanceWorkspaceCreation)
            throw new ConflictException("This approval endpoint only provisions freelance workspaces.");

        if (application.Status == ApplicationRequestStatus.Approved)
            return PlatformApplicationMapper.ToDto(application, application.IdentityAccount.Email, application.IdentityAccount.PhoneNumber);

        if (!ApplicationRequestStateMachine.CanTransition(application.Status, ApplicationRequestStatus.Approved))
            throw new ConflictException("This application cannot be approved.");

        if (!await _context.AppRoles.IgnoreQueryFilters().AnyAsync(
                x => x.TenantId == null && x.Name == SystemRoles.FreelanceOwner && !x.IsDeleted,
                cancellationToken))
        {
            throw new ConflictException("Freelance roles are not seeded yet.");
        }

        var payload = DeserializePayload(application.PayloadJson);
        _context.Entry(application).Property(nameof(ApplicationRequest.RowVersion)).OriginalValue = Convert.FromBase64String(request.RowVersion);
        var workspace = await GetOrCreateProvisioningWorkspace(application, payload, cancellationToken);
        await ProvisionWorkspace(application, workspace, payload, cancellationToken);

        return PlatformApplicationMapper.ToDto(application, application.IdentityAccount.Email, application.IdentityAccount.PhoneNumber);
    }

    private async Task<Tenant> GetOrCreateProvisioningWorkspace(
        ApplicationRequest application,
        FreelanceWorkspaceApplicationPayload payload,
        CancellationToken cancellationToken)
    {
        if (application.ProvisionedWorkspaceId.HasValue)
        {
            return await _context.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == application.ProvisionedWorkspaceId.Value, cancellationToken)
                ?? throw new ConflictException("The provisioned workspace record is missing.");
        }

        var identifierTaken = await _context.Tenants.IgnoreQueryFilters()
            .AnyAsync(x => x.Subdomain == payload.WorkspaceIdentifier, cancellationToken);
        if (identifierTaken)
            throw new ConflictException("The reserved workspace identifier is no longer available.");

        var workspace = new Tenant
        {
            Name = string.IsNullOrWhiteSpace(payload.BrandName) ? payload.WorkspaceName : payload.BrandName,
            Subdomain = payload.WorkspaceIdentifier,
            WorkspaceType = WorkspaceType.FreelanceCoach,
            Status = TenantStatus.Provisioning,
            Email = application.IdentityAccount.Email,
            PhoneNumber = application.IdentityAccount.PhoneNumber,
            Description = payload.Bio,
            LogoUrl = payload.LogoUrl,
            CoverImageUrl = payload.CoverImageUrl,
            BrandingSettings = new BrandingSettings
            {
                AppName = string.IsNullOrWhiteSpace(payload.BrandName) ? payload.WorkspaceName : payload.BrandName,
                LogoUrl = payload.LogoUrl,
                LogoIconUrl = payload.PhotoUrl,
                InvoiceLogoUrl = payload.LogoUrl,
                PrimaryColor = payload.PrimaryColor,
                SecondaryColor = payload.SecondaryColor,
                LoginBackgroundUrl = payload.BackgroundImageUrl,
                DashboardBannerUrl = payload.CoverImageUrl
            }
        };
        _context.Tenants.Add(workspace);
        application.ProvisionedWorkspaceId = workspace.Id;
        await _context.SaveChangesAsync(cancellationToken);
        return workspace;
    }

    private async Task ProvisionWorkspace(
        ApplicationRequest application,
        Tenant workspace,
        FreelanceWorkspaceApplicationPayload payload,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeService.UtcNow;
        var owner = await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == workspace.Id && x.IdentityAccountId == application.IdentityAccountId && !x.IsDeleted, cancellationToken);
        var ownerWasAdded = owner is null;
        if (owner is null)
        {
            owner = new User
            {
                TenantId = workspace.Id,
                IdentityAccountId = application.IdentityAccountId,
                Email = application.IdentityAccount.Email,
                PhoneNumber = application.IdentityAccount.PhoneNumber,
                PasswordHash = application.IdentityAccount.PasswordHash,
                Role = UserRole.FreelanceOwner,
                IsActive = true
            };
            _context.Users.Add(owner);
            _context.UserProfiles.Add(new UserProfile
            {
                UserId = owner.Id,
                FullName = payload.OwnerFullName,
                ProfilePictureUrl = payload.PhotoUrl
            });
        }

        var membership = await _context.WorkspaceMemberships.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.IdentityAccountId == application.IdentityAccountId && x.TenantId == workspace.Id, cancellationToken);
        var membershipWasAdded = membership is null;
        if (membership is null)
        {
            membership = new WorkspaceMembership
            {
                TenantId = workspace.Id,
                IdentityAccountId = application.IdentityAccountId,
                UserId = owner.Id,
                Role = UserRole.FreelanceOwner,
                Status = WorkspaceMembershipStatus.Active,
                ApprovedAt = now,
                ApprovedBy = _currentUserService.UserId
            };
            _context.WorkspaceMemberships.Add(membership);
        }

        var role = await _context.AppRoles.IgnoreQueryFilters()
            .FirstAsync(x => x.TenantId == null && x.Name == SystemRoles.FreelanceOwner && !x.IsDeleted, cancellationToken);
        var roleAssignment = await _context.UserRoleAssignments.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserId == owner.Id && x.RoleId == role.Id, cancellationToken);
        var roleAssignmentWasAdded = roleAssignment is null;
        if (roleAssignment is null)
        {
            roleAssignment = new UserRoleAssignment
            {
                UserId = owner.Id,
                RoleId = role.Id,
                TenantId = workspace.Id
            };
            _context.UserRoleAssignments.Add(roleAssignment);
        }

        var freelanceProfile = await _context.FreelanceWorkspaceProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == workspace.Id, cancellationToken);
        var freelanceProfileWasAdded = freelanceProfile is null;
        if (freelanceProfile is null)
        {
            freelanceProfile = new FreelanceWorkspaceProfile { TenantId = workspace.Id };
            _context.FreelanceWorkspaceProfiles.Add(freelanceProfile);
        }
        freelanceProfile.Bio = payload.Bio;
        freelanceProfile.SpecialtiesJson = JsonSerializer.Serialize(payload.Specialties);
        freelanceProfile.CertificationsJson = JsonSerializer.Serialize(payload.Certifications);
        freelanceProfile.SocialLinksJson = JsonSerializer.Serialize(payload.SocialLinks);
        freelanceProfile.WelcomeMessage = payload.WelcomeMessage;
        freelanceProfile.BookingSettingsJson = payload.BookingSettings?.GetRawText();

        var outbox = new OutboxMessage
        {
            Type = "workspace.application.approved",
            Payload = $"{{\"applicationId\":\"{application.Id}\",\"workspaceId\":\"{workspace.Id}\"}}",
            OccurredAtUtc = now,
            IdempotencyKey = $"application:{application.Id}:approved"
        };
        _context.OutboxMessages.Add(outbox);
        workspace.Status = TenantStatus.Active;
        application.Status = ApplicationRequestStatus.Approved;
        application.DecisionReason = null;
        application.InformationRequest = null;
        application.RequestedFieldsJson = null;
        application.ReviewedAt = now;
        application.ReviewedBy = _currentUserService.UserId;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Preserve the unique provisioning workspace and let Platform Admin retry without a duplicate.
            if (ownerWasAdded) _context.Entry(owner).State = EntityState.Detached;
            if (membershipWasAdded) _context.Entry(membership).State = EntityState.Detached;
            if (roleAssignmentWasAdded) _context.Entry(roleAssignment).State = EntityState.Detached;
            if (freelanceProfileWasAdded) _context.Entry(freelanceProfile).State = EntityState.Detached;
            _context.Entry(outbox).State = EntityState.Detached;
            workspace.Status = TenantStatus.ProvisioningFailed;
            application.Status = ApplicationRequestStatus.UnderReview;
            application.DecisionReason = "Provisioning failed. Platform retry is required.";
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private static FreelanceWorkspaceApplicationPayload DeserializePayload(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<FreelanceWorkspaceApplicationPayload>(payloadJson);
            if (payload is null || string.IsNullOrWhiteSpace(payload.WorkspaceName) || string.IsNullOrWhiteSpace(payload.WorkspaceIdentifier))
                throw new JsonException();
            return payload;
        }
        catch (JsonException)
        {
            throw new ValidationException("Payload", "The freelance application payload is invalid.");
        }
    }
}

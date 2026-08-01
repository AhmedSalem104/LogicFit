using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.ApproveMembershipApplication;

public sealed class ApproveMembershipApplicationCommandHandler
    : IRequestHandler<ApproveMembershipApplicationCommand, PlatformApplicationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IWorkspaceMembershipQuotaService _quotaService;

    public ApproveMembershipApplicationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        IWorkspaceMembershipQuotaService quotaService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _quotaService = quotaService;
    }

    public async Task<PlatformApplicationDto> Handle(ApproveMembershipApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await _context.ApplicationRequests
            .Include(x => x.IdentityAccount)
            .FirstOrDefaultAsync(x => x.Id == request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApplicationRequest), request.ApplicationId);
        if (application.Status == ApplicationRequestStatus.Approved)
            return PlatformApplicationMapper.ToDto(application, application.IdentityAccount.Email, application.IdentityAccount.PhoneNumber);
        if (application.ApplicationType is not (ApplicationType.CoachMembership or ApplicationType.AssistantMembership or ApplicationType.ClientMembership) ||
            !application.TargetWorkspaceId.HasValue || !application.RequestedRole.HasValue)
            throw new ConflictException("This is not a valid membership application.");
        if (!ApplicationRequestStateMachine.CanTransition(application.Status, ApplicationRequestStatus.Approved))
            throw new ConflictException("This application cannot be approved.");

        var workspace = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == application.TargetWorkspaceId.Value && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), application.TargetWorkspaceId.Value);
        if (workspace.WorkspaceType != WorkspaceType.FreelanceCoach)
            throw new ConflictException("This membership application does not target a freelance workspace.");
        if (!MatchesApplicationType(application.ApplicationType, application.RequestedRole.Value))
            throw new ConflictException("The requested role does not match the membership application type.");

        _context.Entry(application).Property(nameof(ApplicationRequest.RowVersion)).OriginalValue = Convert.FromBase64String(request.RowVersion);
        await _quotaService.EnsureCapacityAsync(workspace.Id, application.RequestedRole.Value, cancellationToken);
        // Force the workspace row-version into the same SaveChanges transaction as the new
        // membership. Concurrent approvals then cannot both pass the live capacity check.
        workspace.UpdatedAt = _dateTimeService.UtcNow;

        var existingMembership = await _context.WorkspaceMemberships.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == workspace.Id && x.IdentityAccountId == application.IdentityAccountId && !x.IsDeleted, cancellationToken);
        if (existingMembership)
            throw new ConflictException("The identity already belongs to this workspace.");

        var user = new User
        {
            TenantId = workspace.Id,
            IdentityAccountId = application.IdentityAccountId,
            Email = application.IdentityAccount.Email,
            PhoneNumber = application.IdentityAccount.PhoneNumber,
            PasswordHash = application.IdentityAccount.PasswordHash,
            Role = application.RequestedRole.Value,
            IsActive = true
        };
        _context.Users.Add(user);
        _context.UserProfiles.Add(new UserProfile
        {
            UserId = user.Id,
            FullName = ReadFullName(application.PayloadJson)
        });
        _context.WorkspaceMemberships.Add(new WorkspaceMembership
        {
            TenantId = workspace.Id,
            IdentityAccountId = application.IdentityAccountId,
            UserId = user.Id,
            Role = application.RequestedRole.Value,
            Status = WorkspaceMembershipStatus.Active,
            SponsoredByMembershipId = application.SponsoredByMembershipId,
            ApprovedAt = _dateTimeService.UtcNow,
            ApprovedBy = _currentUserService.UserId
        });
        var roleName = application.RequestedRole.Value switch
        {
            UserRole.FreelanceCoach => SystemRoles.FreelanceCoach,
            UserRole.FreelanceAssistant => SystemRoles.FreelanceAssistant,
            UserRole.Client => SystemRoles.Client,
            _ => throw new ConflictException("Unsupported membership role.")
        };
        var role = await _context.AppRoles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == null && x.Name == roleName && !x.IsDeleted, cancellationToken)
            ?? throw new ConflictException("The requested system role is not seeded.");
        _context.UserRoleAssignments.Add(new UserRoleAssignment { UserId = user.Id, RoleId = role.Id, TenantId = workspace.Id });

        var now = _dateTimeService.UtcNow;
        application.Status = ApplicationRequestStatus.Approved;
        application.DecisionReason = null;
        application.InformationRequest = null;
        application.RequestedFieldsJson = null;
        application.ReviewedAt = now;
        application.ReviewedBy = _currentUserService.UserId;
        _context.OutboxMessages.Add(new OutboxMessage
        {
            Type = "workspace.membership_application.approved",
            Payload = $"{{\"applicationId\":\"{application.Id}\",\"workspaceId\":\"{workspace.Id}\"}}",
            OccurredAtUtc = now,
            IdempotencyKey = $"application:{application.Id}:approved"
        });
        await _context.SaveChangesAsync(cancellationToken);
        return PlatformApplicationMapper.ToDto(application, application.IdentityAccount.Email, application.IdentityAccount.PhoneNumber);
    }

    private static bool MatchesApplicationType(ApplicationType type, UserRole role) =>
        (type == ApplicationType.CoachMembership && role == UserRole.FreelanceCoach) ||
        (type == ApplicationType.AssistantMembership && role == UserRole.FreelanceAssistant) ||
        (type == ApplicationType.ClientMembership && role == UserRole.Client);

    private static string? ReadFullName(string payloadJson)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            return document.RootElement.TryGetProperty("FullName", out var fullName) ? fullName.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.SponsorFreelanceMembership;

public sealed class SponsorFreelanceMembershipCommandHandler
    : IRequestHandler<SponsorFreelanceMembershipCommand, ApplicationTrackingStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IWorkspaceMembershipQuotaService _quotaService;

    public SponsorFreelanceMembershipCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        IWorkspaceMembershipQuotaService quotaService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _quotaService = quotaService;
    }

    public async Task<ApplicationTrackingStatusDto> Handle(SponsorFreelanceMembershipCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = _tenantService.GetCurrentTenantId();
        var workspace = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == workspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), workspaceId);
        if (workspace.WorkspaceType != WorkspaceType.FreelanceCoach)
            throw new ConflictException("Team sponsorship is only available in a freelance workspace.");
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            throw new ForbiddenException("A workspace user is required.");

        var sponsor = await _context.WorkspaceMemberships
            .FirstOrDefaultAsync(x => x.TenantId == workspaceId && x.UserId == currentUserId &&
                                      x.Status == WorkspaceMembershipStatus.Active && !x.IsDeleted, cancellationToken);
        if (sponsor is null || sponsor.Role != UserRole.FreelanceOwner)
            throw new ForbiddenException("Only the Freelance Owner can sponsor a workspace membership.");

        var normalizedEmail = request.IdentityEmail.Trim().ToUpperInvariant();
        var identity = await _context.IdentityAccounts
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("The invitee must first create a LogicFit identity.");
        if (identity.Id == sponsor.IdentityAccountId)
            throw new ConflictException("The workspace owner already has a membership.");

        var applicationType = request.RequestedRole switch
        {
            UserRole.FreelanceCoach => ApplicationType.CoachMembership,
            UserRole.FreelanceAssistant => ApplicationType.AssistantMembership,
            UserRole.Client => ApplicationType.ClientMembership,
            _ => throw new ValidationException("RequestedRole", "Unsupported freelance membership role.")
        };
        var scopeKey = $"workspace:{workspaceId:N}";
        var alreadyMember = await _context.WorkspaceMemberships.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == workspaceId && x.IdentityAccountId == identity.Id && !x.IsDeleted, cancellationToken);
        if (alreadyMember)
            throw new ConflictException("This identity already belongs to the workspace.");
        var duplicateApplication = await _context.ApplicationRequests.AnyAsync(x =>
            x.IdentityAccountId == identity.Id && x.TargetScopeKey == scopeKey && x.ApplicationType == applicationType &&
            (x.Status == ApplicationRequestStatus.Draft || x.Status == ApplicationRequestStatus.Submitted ||
             x.Status == ApplicationRequestStatus.UnderReview || x.Status == ApplicationRequestStatus.NeedsMoreInformation), cancellationToken);
        if (duplicateApplication)
            throw new ConflictException("An active membership application already exists for this workspace.");

        await _quotaService.EnsureCapacityAsync(workspaceId, request.RequestedRole, cancellationToken);
        var now = _dateTimeService.UtcNow;
        var application = new ApplicationRequest
        {
            IdentityAccountId = identity.Id,
            ApplicationType = applicationType,
            Status = ApplicationRequestStatus.Submitted,
            TargetWorkspaceId = workspaceId,
            TargetScopeKey = scopeKey,
            RequestedRole = request.RequestedRole,
            SponsoredByMembershipId = sponsor.Id,
            PayloadJson = JsonSerializer.Serialize(new { FullName = request.FullName.Trim() }),
            SubmittedAt = now
        };
        _context.ApplicationRequests.Add(application);
        _context.ApplicationRequestRevisions.Add(new ApplicationRequestRevision
        {
            ApplicationRequestId = application.Id,
            RevisionNumber = 1,
            PayloadJson = application.PayloadJson,
            SubmittedAt = now,
            SubmittedBy = sponsor.IdentityAccountId.ToString()
        });
        _context.OutboxMessages.Add(new OutboxMessage
        {
            Type = "workspace.membership_application.submitted",
            Payload = $"{{\"applicationId\":\"{application.Id}\",\"workspaceId\":\"{workspaceId}\"}}",
            OccurredAtUtc = now,
            IdempotencyKey = $"application:{application.Id}:submitted"
        });
        await _context.SaveChangesAsync(cancellationToken);

        return new ApplicationTrackingStatusDto
        {
            ApplicationId = application.Id,
            ApplicationType = application.ApplicationType,
            Status = application.Status,
            WorkspaceIdentifier = workspace.Subdomain,
            SubmittedAt = application.SubmittedAt
        };
    }
}

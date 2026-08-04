using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Identity;

/// <summary>Creates the short-lived workspace selection context after verified password authentication.</summary>
public sealed class IdentityWorkspaceSessionIssuer : IIdentityWorkspaceSessionIssuer
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICurrentUserService _currentUserService;

    public IdentityWorkspaceSessionIssuer(IApplicationDbContext context, IDateTimeService dateTimeService, ICurrentUserService currentUserService)
        => (_context, _dateTimeService, _currentUserService) = (context, dateTimeService, currentUserService);

    public async Task<IdentitySignInDto> IssueAsync(Guid identityAccountId, CancellationToken cancellationToken = default)
    {
        var identity = await _context.IdentityAccounts.SingleOrDefaultAsync(x => x.Id == identityAccountId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid credentials");
        var now = _dateTimeService.UtcNow;
        var memberships = await _context.WorkspaceMemberships.IgnoreQueryFilters()
            .Include(x => x.User).Include(x => x.Tenant)
            .Where(x => x.IdentityAccountId == identity.Id && !x.IsDeleted &&
                (x.Status == WorkspaceMembershipStatus.Active ||
                    (x.Status == WorkspaceMembershipStatus.PendingPlatformApproval &&
                     x.Role == UserRole.Owner &&
                     x.Tenant.WorkspaceType == WorkspaceType.Gym &&
                     x.Tenant.Status == TenantStatus.Active)) &&
                x.User.IsActive && !x.User.IsDeleted && !x.Tenant.IsDeleted)
            .OrderBy(x => x.Tenant.Name).ToListAsync(cancellationToken);

        // Older gyms could be activated before the approval handler also repaired the owner's
        // membership. An Active gym is the platform's approval decision, so reconcile only this
        // narrow, owner-only state here. Pending client/workspace approvals must remain blocked.
        foreach (var membership in memberships.Where(x => x.Status == WorkspaceMembershipStatus.PendingPlatformApproval))
        {
            membership.Status = WorkspaceMembershipStatus.Active;
            membership.ApprovedAt ??= now;
            membership.ApprovedBy ??= "identity-login-reconciliation";
        }

        var pendingApplications = await _context.ApplicationRequests
            .Where(x => x.IdentityAccountId == identity.Id && (x.Status == ApplicationRequestStatus.Draft ||
                x.Status == ApplicationRequestStatus.Submitted || x.Status == ApplicationRequestStatus.UnderReview ||
                x.Status == ApplicationRequestStatus.NeedsMoreInformation))
            .OrderByDescending(x => x.SubmittedAt).ToListAsync(cancellationToken);
        var rawSessionToken = IdentityWorkspaceSessionToken.CreateRaw();
        _context.IdentityWorkspaceSessions.Add(new IdentityWorkspaceSession
        {
            IdentityAccountId = identity.Id,
            TokenHash = IdentityWorkspaceSessionToken.Hash(rawSessionToken),
            ExpiresAt = now.AddMinutes(10),
            CreatedByIp = _currentUserService.IpAddress
        });
        identity.LastLoginAt = now;
        await _context.SaveChangesAsync(cancellationToken);
        return new IdentitySignInDto
        {
            WorkspaceSelectionToken = rawSessionToken,
            ExpiresAt = now.AddMinutes(10),
            ActiveWorkspaces = memberships.Select(x => new IdentityWorkspaceDto { WorkspaceId = x.TenantId, Name = x.Tenant.Name, Identifier = x.Tenant.Subdomain, WorkspaceType = x.Tenant.WorkspaceType, WorkspaceStatus = x.Tenant.Status, Role = x.Role }).ToList(),
            PendingApplications = pendingApplications.Select(x => new PendingApplicationDto { ApplicationId = x.Id, ApplicationType = x.ApplicationType, Status = x.Status, SubmittedAt = x.SubmittedAt }).ToList(),
            RequiresWorkspaceSelection = memberships.Count != 1
        };
    }
}

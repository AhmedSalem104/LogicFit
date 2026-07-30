using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EmailTokenGenerator = LogicFit.Application.Features.Identity.IdentityEmailActionToken;

namespace LogicFit.Application.Features.WorkspaceInvites.Commands.AcceptWorkspaceInvite;

/// <summary>Redeems a one-use email-bound invitation only after the recipient has authenticated a verified identity.</summary>
public sealed class AcceptWorkspaceInviteCommandHandler : IRequestHandler<AcceptWorkspaceInviteCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly IWorkspaceMembershipQuotaService _quotaService;

    public AcceptWorkspaceInviteCommandHandler(IApplicationDbContext context, IDateTimeService dateTimeService,
        IWorkspaceMembershipQuotaService quotaService)
        => (_context, _dateTimeService, _quotaService) = (context, dateTimeService, quotaService);

    public async Task Handle(AcceptWorkspaceInviteCommand request, CancellationToken cancellationToken)
    {
        var session = await IdentityWorkspaceSessionResolver.GetActiveAsync(
            _context, _dateTimeService, request.WorkspaceSelectionToken, cancellationToken);
        var identity = await _context.IdentityAccounts.SingleOrDefaultAsync(x => x.Id == session.IdentityAccountId, cancellationToken)
            ?? throw new UnauthorizedException("Identity session is invalid.");
        if (!identity.IsActive || identity.EmailVerifiedAt is null)
            throw new UnauthorizedException("A verified active identity is required to accept this invitation.");

        var now = _dateTimeService.UtcNow;
        var invite = await _context.WorkspaceInvites
            .Include(x => x.Tenant)
            .SingleOrDefaultAsync(x => x.TokenHash == EmailTokenGenerator.Hash(request.Token), cancellationToken);
        if (invite is null || invite.Status != WorkspaceInviteStatus.Pending || invite.ExpiresAt <= now)
            throw new ConflictException("This invitation is invalid, expired, or has already been used.");
        if (!string.Equals(invite.NormalizedEmail, identity.NormalizedEmail, StringComparison.Ordinal))
            throw new ForbiddenException("Sign in with the invited email address to accept this invitation.");
        if (invite.Tenant.IsDeleted || invite.Tenant.Status is TenantStatus.Suspended or TenantStatus.Archived or TenantStatus.ProvisioningFailed)
            throw new ConflictException("This workspace is not available for new memberships.");

        var existingMembership = await _context.WorkspaceMemberships.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == invite.TenantId && x.IdentityAccountId == identity.Id && !x.IsDeleted, cancellationToken);
        if (existingMembership)
            throw new ConflictException("This identity already belongs to the workspace.");
        await _quotaService.EnsureCapacityAsync(invite.TenantId, invite.Role, cancellationToken);
        // Makes concurrent invitation accepts conflict at the workspace row-version after their quota checks.
        invite.Tenant.UpdatedAt = now;

        var user = new User
        {
            TenantId = invite.TenantId,
            IdentityAccountId = identity.Id,
            Email = identity.Email,
            PhoneNumber = identity.PhoneNumber,
            PasswordHash = identity.PasswordHash,
            Role = invite.Role,
            IsActive = true
        };
        _context.Users.Add(user);
        _context.UserProfiles.Add(new UserProfile { UserId = user.Id, FullName = identity.FullName });
        _context.WorkspaceMemberships.Add(new WorkspaceMembership
        {
            TenantId = invite.TenantId,
            IdentityAccountId = identity.Id,
            UserId = user.Id,
            Role = invite.Role,
            Status = WorkspaceMembershipStatus.Active,
            SponsoredByMembershipId = invite.InvitedByMembershipId,
            ApprovedAt = now,
            ApprovedBy = invite.InvitedByMembershipId.ToString()
        });
        var systemRole = WorkspaceInviteSupport.SystemRoleFor(invite.Role);
        var role = await _context.AppRoles.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.TenantId == null && x.Name == systemRole && !x.IsDeleted, cancellationToken)
            ?? throw new ConflictException("The requested system role is not seeded.");
        _context.UserRoleAssignments.Add(new UserRoleAssignment { UserId = user.Id, RoleId = role.Id, TenantId = invite.TenantId });
        invite.Status = WorkspaceInviteStatus.Accepted;
        invite.AcceptedAt = now;
        invite.AcceptedIdentityAccountId = identity.Id;
        _context.OutboxMessages.Add(new OutboxMessage
        {
            Type = "workspace.invite.accepted",
            Payload = $"{{\"inviteId\":\"{invite.Id}\",\"workspaceId\":\"{invite.TenantId}\",\"identityAccountId\":\"{identity.Id}\"}}",
            OccurredAtUtc = now,
            IdempotencyKey = $"workspace-invite:{invite.Id}:accepted"
        });
        await _context.SaveChangesAsync(cancellationToken);
    }
}

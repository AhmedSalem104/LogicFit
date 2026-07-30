using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity;
using LogicFit.Application.Features.WorkspaceClientJoins.DTOs;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EmailTokenGenerator = LogicFit.Application.Features.Identity.IdentityEmailActionToken;

namespace LogicFit.Application.Features.WorkspaceClientJoins.Commands.JoinWorkspaceAsClient;

public sealed class JoinWorkspaceAsClientCommandHandler : IRequestHandler<JoinWorkspaceAsClientCommand, ClientJoinResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly IWorkspaceMembershipQuotaService _quotaService;
    private readonly IRbacService _rbacService;

    public JoinWorkspaceAsClientCommandHandler(IApplicationDbContext context, IDateTimeService dateTimeService,
        IWorkspaceMembershipQuotaService quotaService, IRbacService rbacService)
        => (_context, _dateTimeService, _quotaService, _rbacService) = (context, dateTimeService, quotaService, rbacService);

    public async Task<ClientJoinResultDto> Handle(JoinWorkspaceAsClientCommand request, CancellationToken cancellationToken)
    {
        var session = await IdentityWorkspaceSessionResolver.GetActiveAsync(_context, _dateTimeService, request.WorkspaceSelectionToken, cancellationToken);
        var identity = await _context.IdentityAccounts.SingleOrDefaultAsync(x => x.Id == session.IdentityAccountId, cancellationToken)
            ?? throw new UnauthorizedException("Identity session is invalid.");
        if (!identity.IsActive || identity.EmailVerifiedAt is null)
            throw new UnauthorizedException("A verified active identity is required to join a workspace.");

        var now = _dateTimeService.UtcNow;
        var joinCode = await _context.WorkspaceClientJoinCodes
            .Include(x => x.Tenant)
            .SingleOrDefaultAsync(x => x.CodeHash == EmailTokenGenerator.Hash(request.Code), cancellationToken);
        if (joinCode is null || joinCode.RevokedAt.HasValue || joinCode.ExpiresAt <= now || joinCode.Tenant.IsDeleted ||
            joinCode.Tenant.Status is TenantStatus.Suspended or TenantStatus.Archived or TenantStatus.ProvisioningFailed)
            throw new ConflictException("This join code is invalid, expired, or its workspace is unavailable.");
        var existing = await _context.WorkspaceMemberships.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == joinCode.TenantId && x.IdentityAccountId == identity.Id && !x.IsDeleted, cancellationToken);
        if (existing)
            throw new ConflictException("This identity already belongs to the workspace.");

        await _quotaService.EnsureCapacityAsync(joinCode.TenantId, UserRole.Client, cancellationToken);
        joinCode.Tenant.UpdatedAt = now;
        var status = joinCode.AutoApproveClients ? WorkspaceMembershipStatus.Active : WorkspaceMembershipStatus.PendingWorkspaceApproval;
        var user = new User
        {
            TenantId = joinCode.TenantId,
            IdentityAccountId = identity.Id,
            Email = identity.Email,
            PhoneNumber = identity.PhoneNumber,
            PasswordHash = identity.PasswordHash,
            Role = UserRole.Client,
            IsActive = true
        };
        _context.Users.Add(user);
        _context.UserProfiles.Add(new UserProfile { UserId = user.Id, FullName = identity.FullName });
        var membership = new WorkspaceMembership
        {
            TenantId = joinCode.TenantId,
            IdentityAccountId = identity.Id,
            UserId = user.Id,
            Role = UserRole.Client,
            Status = status,
            ApprovedAt = status == WorkspaceMembershipStatus.Active ? now : null
        };
        _context.WorkspaceMemberships.Add(membership);
        await _rbacService.EnsureUserInRoleAsync(user.Id, joinCode.TenantId, SystemRoles.Client, cancellationToken);
        _context.OutboxMessages.Add(new OutboxMessage
        {
            Type = status == WorkspaceMembershipStatus.Active ? "workspace.client_join.approved" : "workspace.client_join.pending_approval",
            Payload = $"{{\"workspaceId\":\"{joinCode.TenantId}\",\"membershipId\":\"{membership.Id}\"}}",
            OccurredAtUtc = now,
            IdempotencyKey = $"workspace-client-join:{joinCode.Id}:{identity.Id}"
        });
        await _context.SaveChangesAsync(cancellationToken);
        return new ClientJoinResultDto { WorkspaceId = joinCode.TenantId, MembershipStatus = status };
    }
}

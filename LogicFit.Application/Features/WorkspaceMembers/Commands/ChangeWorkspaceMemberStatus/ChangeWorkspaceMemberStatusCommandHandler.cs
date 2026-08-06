using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.WorkspaceMembers.DTOs;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceMembers.Commands.ChangeWorkspaceMemberStatus;

public sealed class ChangeWorkspaceMemberStatusCommandHandler : IRequestHandler<ChangeWorkspaceMemberStatusCommand, WorkspaceMemberDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IRbacService _rbacService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _clock;

    public ChangeWorkspaceMemberStatusCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        IRbacService rbacService,
        ICurrentUserService currentUser,
        IDateTimeService clock)
        => (_context, _tenantService, _rbacService, _currentUser, _clock) = (context, tenantService, rbacService, currentUser, clock);

    public async Task<WorkspaceMemberDto> Handle(ChangeWorkspaceMemberStatusCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.CurrentTenantId ?? throw new ForbiddenException("A workspace context is required.");
        var membership = await _context.WorkspaceMemberships.IgnoreQueryFilters()
            .Include(x => x.User).ThenInclude(x => x.Profile)
            .Include(x => x.IdentityAccount)
            .FirstOrDefaultAsync(x => x.Id == request.MembershipId && x.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("WorkspaceMembership", request.MembershipId);

        var eventName = request.Action switch
        {
            WorkspaceMemberStatusAction.Suspend => "WorkspaceMemberSuspended",
            WorkspaceMemberStatusAction.Activate => "WorkspaceMemberActivated",
            WorkspaceMemberStatusAction.Remove => "WorkspaceMemberRemoved",
            _ => throw new ValidationException("Action", "Unknown workspace member action.")
        };

        switch (request.Action)
        {
            case WorkspaceMemberStatusAction.Suspend:
                membership.Status = WorkspaceMembershipStatus.Suspended;
                membership.User.IsActive = false;
                break;
            case WorkspaceMemberStatusAction.Activate:
                if (!membership.IdentityAccount.IsActive)
                    throw new ConflictException("WORKSPACE_MEMBER_IDENTITY_INACTIVE", "The global identity is inactive.");
                membership.Status = WorkspaceMembershipStatus.Active;
                membership.IsDeleted = false;
                membership.DeletedAt = null;
                membership.DeletedBy = null;
                membership.User.IsDeleted = false;
                membership.User.IsActive = true;
                await _rbacService.EnsureUserInRoleAsync(membership.UserId, tenantId, WorkspaceMemberMapping.RoleName(membership.Role), cancellationToken);
                break;
            case WorkspaceMemberStatusAction.Remove:
                membership.Status = WorkspaceMembershipStatus.Revoked;
                membership.IsDeleted = true;
                membership.DeletedAt = _clock.UtcNow;
                membership.DeletedBy = _currentUser.UserId;
                membership.User.IsActive = false;
                membership.User.IsDeleted = true;
                membership.User.DeletedAt = _clock.UtcNow;
                membership.User.DeletedBy = _currentUser.UserId;
                break;
        }

        SecurityAuditLog.Add(_context, _currentUser, _clock, eventName, true, membership.UserId, tenantId);
        await _context.SaveChangesAsync(cancellationToken);
        return WorkspaceMemberMapping.ToDto(membership, _clock.UtcNow);
    }
}

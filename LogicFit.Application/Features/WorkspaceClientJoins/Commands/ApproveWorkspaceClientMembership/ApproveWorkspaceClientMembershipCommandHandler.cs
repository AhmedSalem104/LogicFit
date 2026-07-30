using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceClientJoins.Commands.ApproveWorkspaceClientMembership;

public sealed class ApproveWorkspaceClientMembershipCommandHandler : IRequestHandler<ApproveWorkspaceClientMembershipCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IWorkspaceMembershipQuotaService _quotaService;

    public ApproveWorkspaceClientMembershipCommandHandler(IApplicationDbContext context, ITenantService tenantService,
        ICurrentUserService currentUserService, IDateTimeService dateTimeService, IWorkspaceMembershipQuotaService quotaService)
        => (_context, _tenantService, _currentUserService, _dateTimeService, _quotaService)
            = (context, tenantService, currentUserService, dateTimeService, quotaService);

    public async Task Handle(ApproveWorkspaceClientMembershipCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var membership = await _context.WorkspaceMemberships
            .FirstOrDefaultAsync(x => x.Id == request.MembershipId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("WorkspaceMembership", request.MembershipId);
        if (membership.Role != UserRole.Client || membership.Status != WorkspaceMembershipStatus.PendingWorkspaceApproval)
            throw new ConflictException("This client membership is not awaiting workspace approval.");
        await _quotaService.EnsureCapacityAsync(tenantId, UserRole.Client, cancellationToken);
        var workspace = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken)
            ?? throw new NotFoundException("Tenant", tenantId);
        var now = _dateTimeService.UtcNow;
        workspace.UpdatedAt = now;
        membership.Status = WorkspaceMembershipStatus.Active;
        membership.ApprovedAt = now;
        membership.ApprovedBy = _currentUserService.UserId;
        _context.OutboxMessages.Add(new LogicFit.Domain.Entities.OutboxMessage
        {
            Type = "workspace.client_join.approved",
            Payload = $"{{\"workspaceId\":\"{tenantId}\",\"membershipId\":\"{membership.Id}\"}}",
            OccurredAtUtc = now,
            IdempotencyKey = $"workspace-client-membership:{membership.Id}:approved"
        });
        await _context.SaveChangesAsync(cancellationToken);
    }
}

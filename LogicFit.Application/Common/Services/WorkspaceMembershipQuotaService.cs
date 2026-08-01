using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Common.Services;

/// <summary>Live quota check for proposed freelance memberships; approval calls it again.</summary>
public sealed class WorkspaceMembershipQuotaService : IWorkspaceMembershipQuotaService
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public WorkspaceMembershipQuotaService(IApplicationDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task EnsureCapacityAsync(Guid workspaceId, UserRole requestedRole, CancellationToken cancellationToken = default)
    {
        var now = _dateTimeService.UtcNow;
        var subscription = await _context.TenantSubscriptions
            .Where(x => x.TenantId == workspaceId &&
                        (x.Status == TenantSubscriptionStatus.Trial || x.Status == TenantSubscriptionStatus.Active ||
                         x.Status == TenantSubscriptionStatus.PastDue || x.Status == TenantSubscriptionStatus.GracePeriod) &&
                        (x.EndDate == null || x.EndDate > now))
            .OrderByDescending(x => x.EndDate)
            .FirstOrDefaultAsync(cancellationToken);
        if (subscription is null)
            throw new PlanLimitExceededException("WORKSPACE_SUBSCRIPTION_REQUIRED", "An active workspace subscription is required before adding team members.");

        var plan = await _context.Plans.FirstOrDefaultAsync(x => x.Id == subscription.PlanId, cancellationToken);
        if (plan is null)
            throw new PlanLimitExceededException("WORKSPACE_SUBSCRIPTION_REQUIRED", "The workspace subscription plan is unavailable.");

        if (requestedRole == UserRole.Client)
        {
            if (!plan.MaxMembers.HasValue) return;
            var clients = await _context.WorkspaceMemberships.IgnoreQueryFilters()
                .CountAsync(x => x.TenantId == workspaceId && x.Status == WorkspaceMembershipStatus.Active &&
                                 x.Role == UserRole.Client && !x.IsDeleted, cancellationToken);
            if (clients >= plan.MaxMembers.Value)
                throw new PlanLimitExceededException("PLAN_CLIENT_LIMIT_REACHED", "The workspace client limit has been reached.");
            return;
        }

        if (!plan.MaxCoaches.HasValue) return;
        var team = await _context.WorkspaceMemberships.IgnoreQueryFilters()
            .CountAsync(x => x.TenantId == workspaceId && x.Status == WorkspaceMembershipStatus.Active &&
                             (x.Role == UserRole.FreelanceCoach || x.Role == UserRole.FreelanceAssistant) && !x.IsDeleted,
                cancellationToken);
        if (team >= plan.MaxCoaches.Value)
            throw new PlanLimitExceededException("PLAN_MEMBER_LIMIT_REACHED", "The workspace team-member limit has been reached.");
    }
}

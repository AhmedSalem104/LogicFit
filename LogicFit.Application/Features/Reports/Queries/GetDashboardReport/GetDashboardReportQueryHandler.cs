using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetDashboardReport;

public class GetDashboardReportQueryHandler : IRequestHandler<GetDashboardReportQuery, DashboardReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetDashboardReportQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<DashboardReportDto> Handle(GetDashboardReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var in7Days = now.AddDays(7);

        var totalClients = await _context.Users
            .CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Client && !u.IsDeleted, cancellationToken);

        var activeClients = await _context.Users
            .CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Client && u.IsActive && !u.IsDeleted, cancellationToken);

        var newClientsThisMonth = await _context.Users
            .CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Client && u.CreatedAt >= startOfMonth && !u.IsDeleted, cancellationToken);

        var totalCoaches = await _context.Users
            .CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Coach && !u.IsDeleted, cancellationToken);

        var activeSubscriptions = await _context.ClientSubscriptions
            .CountAsync(cs => cs.TenantId == tenantId && cs.Status == SubscriptionStatus.Active && !cs.IsDeleted, cancellationToken);

        var expiringSubscriptions = await _context.ClientSubscriptions
            .CountAsync(cs => cs.TenantId == tenantId && cs.Status == SubscriptionStatus.Active && cs.EndDate <= in7Days && !cs.IsDeleted, cancellationToken);

        // Calculate revenue from subscription plan prices
        var subscriptionsThisMonth = await _context.ClientSubscriptions
            .Include(cs => cs.Plan)
            .Where(cs => cs.TenantId == tenantId && cs.StartDate >= startOfMonth && !cs.IsDeleted)
            .ToListAsync(cancellationToken);
        var revenueThisMonth = subscriptionsThisMonth.Sum(cs => cs.Plan?.Price ?? 0);

        var subscriptionsLastMonth = await _context.ClientSubscriptions
            .Include(cs => cs.Plan)
            .Where(cs => cs.TenantId == tenantId && cs.StartDate >= startOfLastMonth && cs.StartDate < startOfMonth && !cs.IsDeleted)
            .ToListAsync(cancellationToken);
        var revenueLastMonth = subscriptionsLastMonth.Sum(cs => cs.Plan?.Price ?? 0);

        var workoutsThisMonth = await _context.WorkoutSessions
            .CountAsync(ws => ws.TenantId == tenantId && ws.StartedAt >= startOfMonth && !ws.IsDeleted, cancellationToken);

        var activeDietPlans = await _context.DietPlans
            .CountAsync(dp => dp.TenantId == tenantId && dp.Status == PlanStatus.Active && !dp.IsDeleted, cancellationToken);

        return new DashboardReportDto
        {
            TotalClients = totalClients,
            ActiveClients = activeClients,
            NewClientsThisMonth = newClientsThisMonth,
            TotalCoaches = totalCoaches,
            ActiveSubscriptions = activeSubscriptions,
            ExpiringSubscriptions = expiringSubscriptions,
            TotalRevenueThisMonth = revenueThisMonth,
            TotalRevenueLastMonth = revenueLastMonth,
            TotalWorkoutsThisMonth = workoutsThisMonth,
            TotalDietPlansActive = activeDietPlans
        };
    }
}

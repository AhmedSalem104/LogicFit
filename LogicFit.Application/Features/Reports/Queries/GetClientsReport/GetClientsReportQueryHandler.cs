using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetClientsReport;

public class GetClientsReportQueryHandler : IRequestHandler<GetClientsReportQuery, ClientsReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetClientsReportQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<ClientsReportDto> Handle(GetClientsReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var totalClients = await _context.Users
            .CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Client && !u.IsDeleted, cancellationToken);

        var activeClients = await _context.Users
            .CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Client && u.IsActive && !u.IsDeleted, cancellationToken);

        var inactiveClients = totalClients - activeClients;

        var newClientsThisMonth = await _context.Users
            .CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Client && u.CreatedAt >= startOfMonth && !u.IsDeleted, cancellationToken);

        var clientsWithActiveSubscription = await _context.ClientSubscriptions
            .Where(cs => cs.TenantId == tenantId && cs.Status == SubscriptionStatus.Active && !cs.IsDeleted)
            .Select(cs => cs.ClientId)
            .Distinct()
            .CountAsync(cancellationToken);

        var clientsWithoutSubscription = totalClients - clientsWithActiveSubscription;

        // Top clients by sessions
        var topClients = await _context.Users
            .Include(u => u.Profile)
            .Include(u => u.WorkoutSessions)
            .Include(u => u.Subscriptions)
                .ThenInclude(s => s.Plan)
            .Where(u => u.TenantId == tenantId && u.Role == UserRole.Client && !u.IsDeleted)
            .OrderByDescending(u => u.WorkoutSessions.Count(ws => !ws.IsDeleted))
            .Take(10)
            .Select(u => new ClientSummaryDto
            {
                Id = u.Id,
                Name = u.Profile != null ? u.Profile.FullName ?? u.Email : u.Email,
                PhoneNumber = u.PhoneNumber,
                TotalSessions = u.WorkoutSessions.Count(ws => !ws.IsDeleted),
                TotalPaid = u.Subscriptions.Where(s => !s.IsDeleted).Sum(s => s.Plan.Price)
            })
            .ToListAsync(cancellationToken);

        // Monthly trend (last 6 months)
        var monthlyTrend = new List<MonthlyClientDto>();
        for (int i = 5; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);

            var newClients = await _context.Users
                .CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Client &&
                            u.CreatedAt >= monthStart && u.CreatedAt < monthEnd && !u.IsDeleted, cancellationToken);

            var churned = await _context.Users
                .CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Client &&
                            u.DeletedAt >= monthStart && u.DeletedAt < monthEnd && u.IsDeleted, cancellationToken);

            monthlyTrend.Add(new MonthlyClientDto
            {
                Month = monthStart.ToString("MMM yyyy"),
                NewClients = newClients,
                ChurnedClients = churned
            });
        }

        return new ClientsReportDto
        {
            TotalClients = totalClients,
            ActiveClients = activeClients,
            InactiveClients = inactiveClients,
            NewClientsThisMonth = newClientsThisMonth,
            ClientsWithActiveSubscription = clientsWithActiveSubscription,
            ClientsWithoutSubscription = clientsWithoutSubscription,
            TopClients = topClients,
            MonthlyTrend = monthlyTrend
        };
    }
}

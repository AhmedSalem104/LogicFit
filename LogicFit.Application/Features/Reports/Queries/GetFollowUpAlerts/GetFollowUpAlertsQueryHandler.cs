using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetFollowUpAlerts;

public sealed class GetFollowUpAlertsQueryHandler : IRequestHandler<GetFollowUpAlertsQuery, FollowUpAlertsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetFollowUpAlertsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<FollowUpAlertsDto> Handle(GetFollowUpAlertsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = DateTime.UtcNow;
        var today = now.Date;
        var expiringWithinDays = Math.Clamp(request.ExpiringWithinDays, 1, 30);
        var inactiveAfterDays = Math.Clamp(request.InactiveAfterDays, 1, 90);
        var limit = Math.Clamp(request.Limit, 1, 200);

        var clients = await _context.Users
            .AsNoTracking()
            .Where(user => user.TenantId == tenantId && user.Role == UserRole.Client && !user.IsDeleted)
            .Select(user => new
            {
                user.Id,
                Name = user.Profile != null && user.Profile.FullName != null
                    ? user.Profile.FullName
                    : user.Email,
                user.PhoneNumber
            })
            .ToListAsync(cancellationToken);

        if (clients.Count == 0)
        {
            return new FollowUpAlertsDto { GeneratedAtUtc = now };
        }

        var clientIds = clients.Select(client => client.Id).ToArray();
        var subscriptionRows = await _context.ClientSubscriptions
            .AsNoTracking()
            .Where(subscription => subscription.TenantId == tenantId
                && clientIds.Contains(subscription.ClientId)
                && !subscription.IsDeleted)
            .Select(subscription => new
            {
                subscription.ClientId,
                subscription.Status,
                subscription.EndDate,
                AmountRemaining = subscription.TotalAmount > subscription.AmountPaid
                    ? subscription.TotalAmount - subscription.AmountPaid
                    : 0m
            })
            .ToListAsync(cancellationToken);

        var subscriptions = subscriptionRows
            .Select(subscription => new SubscriptionSnapshot(
                subscription.ClientId,
                subscription.Status,
                subscription.EndDate,
                subscription.AmountRemaining))
            .ToList();

        var lastVisits = await _context.Attendances
            .AsNoTracking()
            .Where(attendance => attendance.TenantId == tenantId
                && clientIds.Contains(attendance.ClientId))
            .GroupBy(attendance => attendance.ClientId)
            .Select(group => new { ClientId = group.Key, LastVisit = group.Max(attendance => attendance.CheckInTime) })
            .ToDictionaryAsync(item => item.ClientId, item => item.LastVisit, cancellationToken);

        var subscriptionsByClient = subscriptions
            .GroupBy(subscription => subscription.ClientId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var candidates = new List<FollowUpAlertItemDto>();
        foreach (var client in clients)
        {
            subscriptionsByClient.TryGetValue(client.Id, out var clientSubscriptions);
            var current = clientSubscriptions?
                .OrderByDescending(subscription => IsOperational(subscription.Status))
                .ThenByDescending(subscription => subscription.EndDate)
                .FirstOrDefault();

            var hasLastVisit = lastVisits.TryGetValue(client.Id, out var lastVisit);
            var daysSinceLastVisit = hasLastVisit
                ? Math.Max(0, (today - lastVisit.Date).Days)
                : (int?)null;

            var alert = CreateSubscriptionAlert(client.Id, client.Name, client.PhoneNumber, current, today, expiringWithinDays);
            if (alert is null && current is not null && IsOperational(current.Status)
                && (!hasLastVisit || lastVisit < now.AddDays(-inactiveAfterDays)))
            {
                alert = new FollowUpAlertItemDto
                {
                    ClientId = client.Id,
                    ClientName = client.Name ?? string.Empty,
                    PhoneNumber = client.PhoneNumber,
                    Kind = "inactive",
                    Status = current.Status.ToString(),
                    EndDate = current.EndDate,
                    DaysSinceLastVisit = daysSinceLastVisit,
                    Priority = 3
                };
            }

            if (alert is not null)
            {
                alert.DaysSinceLastVisit = daysSinceLastVisit;
                candidates.Add(alert);
            }
        }

        var alerts = candidates
            .OrderBy(alert => alert.Priority)
            .ThenBy(alert => alert.EndDate ?? DateTime.MaxValue)
            .ThenBy(alert => alert.ClientName)
            .Take(limit)
            .ToList();

        return new FollowUpAlertsDto
        {
            GeneratedAtUtc = now,
            Alerts = alerts
        };
    }

    private static FollowUpAlertItemDto? CreateSubscriptionAlert(
        Guid clientId,
        string? clientName,
        string? phoneNumber,
        SubscriptionSnapshot? subscription,
        DateTime today,
        int expiringWithinDays)
    {
        if (subscription is null)
            return null;

        var remainingDays = (subscription.EndDate.Date - today).Days;
        if (subscription.AmountRemaining > 0m && subscription.Status is not SubscriptionStatus.Cancelled)
        {
            return new FollowUpAlertItemDto
            {
                ClientId = clientId,
                ClientName = clientName ?? string.Empty,
                PhoneNumber = phoneNumber,
                Kind = "debt",
                Status = subscription.Status.ToString(),
                EndDate = subscription.EndDate,
                AmountRemaining = subscription.AmountRemaining,
                Priority = 1
            };
        }

        if ((subscription.Status is SubscriptionStatus.Active or SubscriptionStatus.Trial)
            && remainingDays >= 0 && remainingDays <= expiringWithinDays)
        {
            return new FollowUpAlertItemDto
            {
                ClientId = clientId,
                ClientName = clientName ?? string.Empty,
                PhoneNumber = phoneNumber,
                Kind = "expiring",
                Status = subscription.Status.ToString(),
                EndDate = subscription.EndDate,
                Priority = remainingDays <= 3 ? 1 : 2
            };
        }

        if ((subscription.Status is SubscriptionStatus.Active or SubscriptionStatus.Expired) && remainingDays < 0)
        {
            return new FollowUpAlertItemDto
            {
                ClientId = clientId,
                ClientName = clientName ?? string.Empty,
                PhoneNumber = phoneNumber,
                Kind = "expired",
                Status = subscription.Status.ToString(),
                EndDate = subscription.EndDate,
                Priority = 1
            };
        }

        return null;
    }

    private static bool IsOperational(SubscriptionStatus status)
        => status is SubscriptionStatus.Active or SubscriptionStatus.Trial;

    private sealed record SubscriptionSnapshot(
        Guid ClientId,
        SubscriptionStatus Status,
        DateTime EndDate,
        decimal AmountRemaining);
}

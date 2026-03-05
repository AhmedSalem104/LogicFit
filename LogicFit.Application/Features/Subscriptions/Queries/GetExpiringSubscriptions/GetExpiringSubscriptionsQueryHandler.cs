using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Subscriptions.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Queries.GetExpiringSubscriptions;

public class GetExpiringSubscriptionsQueryHandler : IRequestHandler<GetExpiringSubscriptionsQuery, List<ClientSubscriptionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetExpiringSubscriptionsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<ClientSubscriptionDto>> Handle(GetExpiringSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = DateTime.UtcNow;
        var expiryDate = now.AddDays(request.Days);

        return await _context.ClientSubscriptions
            .Include(s => s.Client).ThenInclude(c => c.Profile)
            .Include(s => s.Plan)
            .Include(s => s.SalesCoach).ThenInclude(c => c!.Profile)
            .Where(s => s.TenantId == tenantId
                && s.Status == SubscriptionStatus.Active
                && s.EndDate <= expiryDate
                && s.EndDate > now)
            .OrderBy(s => s.EndDate)
            .Select(s => new ClientSubscriptionDto
            {
                Id = s.Id,
                TenantId = s.TenantId,
                ClientId = s.ClientId,
                ClientName = s.Client.Profile != null ? s.Client.Profile.FullName : s.Client.Email,
                PlanId = s.PlanId,
                PlanName = s.Plan.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Status = s.Status,
                SalesCoachId = s.SalesCoachId,
                SalesCoachName = s.SalesCoach != null
                    ? (s.SalesCoach.Profile != null ? s.SalesCoach.Profile.FullName : s.SalesCoach.Email)
                    : null,
                PaymentMethod = s.PaymentMethod,
                TotalAmount = s.TotalAmount,
                AmountPaid = s.AmountPaid,
                Discount = s.Discount,
                Notes = s.Notes,
                RenewedFromId = s.RenewedFromId
            })
            .ToListAsync(cancellationToken);
    }
}

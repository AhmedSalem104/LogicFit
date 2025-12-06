using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Subscriptions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Queries.GetClientSubscriptions;

public class GetClientSubscriptionsQueryHandler : IRequestHandler<GetClientSubscriptionsQuery, List<ClientSubscriptionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetClientSubscriptionsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<ClientSubscriptionDto>> Handle(GetClientSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.ClientSubscriptions
            .Include(s => s.Client).ThenInclude(c => c.Profile)
            .Include(s => s.Plan)
            .Include(s => s.SalesCoach).ThenInclude(c => c!.Profile)
            .Include(s => s.Freezes)
            .Where(s => s.TenantId == tenantId)
            .AsQueryable();

        if (request.ClientId.HasValue)
            query = query.Where(s => s.ClientId == request.ClientId.Value);

        if (request.Status.HasValue)
            query = query.Where(s => s.Status == request.Status.Value);

        return await query
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
                SalesCoachName = s.SalesCoach != null ? (s.SalesCoach.Profile != null ? s.SalesCoach.Profile.FullName : s.SalesCoach.Email) : null,
                Freezes = s.Freezes.Select(f => new SubscriptionFreezeDto
                {
                    Id = f.Id,
                    SubscriptionId = f.SubscriptionId,
                    StartDate = f.StartDate,
                    EndDate = f.EndDate,
                    Reason = f.Reason
                }).ToList()
            })
            .ToListAsync(cancellationToken);
    }
}

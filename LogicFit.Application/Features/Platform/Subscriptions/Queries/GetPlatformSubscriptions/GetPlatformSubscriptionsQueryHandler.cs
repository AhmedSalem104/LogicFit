using LogicFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Subscriptions.Queries.GetPlatformSubscriptions;

public class GetPlatformSubscriptionsQueryHandler
    : IRequestHandler<GetPlatformSubscriptionsQuery, List<PlatformSubscriptionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPlatformSubscriptionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PlatformSubscriptionDto>> Handle(GetPlatformSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.TenantSubscriptions.AsQueryable();
        if (request.Status.HasValue)
        {
            query = query.Where(s => s.Status == request.Status.Value);
        }

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new PlatformSubscriptionDto
            {
                Id = s.Id,
                TenantId = s.TenantId,
                TenantName = s.Tenant.Name,
                PlanId = s.PlanId,
                PlanName = s.Plan.Name,
                Status = s.Status,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                TrialEndsAt = s.TrialEndsAt,
                Amount = s.Amount,
                Currency = s.Currency,
                AutoRenew = s.AutoRenew
            })
            .ToListAsync(cancellationToken);
    }
}

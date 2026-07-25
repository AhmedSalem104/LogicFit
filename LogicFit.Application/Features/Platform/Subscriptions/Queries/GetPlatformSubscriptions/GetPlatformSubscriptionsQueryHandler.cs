using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Subscriptions.Queries.GetPlatformSubscriptions;

public class GetPlatformSubscriptionsQueryHandler
    : IRequestHandler<GetPlatformSubscriptionsQuery, PagedResult<PlatformSubscriptionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPlatformSubscriptionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<PlatformSubscriptionDto>> Handle(GetPlatformSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.TenantSubscriptions.AsQueryable();
        if (request.Status.HasValue)
        {
            query = query.Where(s => s.Status == request.Status.Value);
        }

        var (page, pageSize) = PageRequest.Normalize(request.Page, request.PageSize);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
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
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<PlatformSubscriptionDto>.Create(items, totalCount, page, pageSize);
    }
}

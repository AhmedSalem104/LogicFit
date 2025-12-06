using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Subscriptions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Queries.GetSubscriptionPlans;

public class GetSubscriptionPlansQueryHandler : IRequestHandler<GetSubscriptionPlansQuery, List<SubscriptionPlanDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetSubscriptionPlansQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<SubscriptionPlanDto>> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        return await _context.SubscriptionPlans
            .Where(p => p.TenantId == tenantId)
            .Select(p => new SubscriptionPlanDto
            {
                Id = p.Id,
                TenantId = p.TenantId,
                Name = p.Name,
                Price = p.Price,
                DurationMonths = p.DurationMonths
            })
            .ToListAsync(cancellationToken);
    }
}

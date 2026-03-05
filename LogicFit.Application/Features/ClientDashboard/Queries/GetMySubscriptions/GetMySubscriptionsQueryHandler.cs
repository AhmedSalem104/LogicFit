using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.ClientDashboard.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ClientDashboard.Queries.GetMySubscriptions;

public class GetMySubscriptionsQueryHandler : IRequestHandler<GetMySubscriptionsQuery, List<MySubscriptionSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetMySubscriptionsQueryHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<List<MySubscriptionSummaryDto>> Handle(GetMySubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var userId = Guid.Parse(_currentUserService.UserId!);

        return await _context.ClientSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.TenantId == tenantId && s.ClientId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new MySubscriptionSummaryDto
            {
                Id = s.Id,
                PlanName = s.Plan.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Status = s.Status,
                TotalAmount = s.TotalAmount,
                AmountPaid = s.AmountPaid
            })
            .ToListAsync(cancellationToken);
    }
}

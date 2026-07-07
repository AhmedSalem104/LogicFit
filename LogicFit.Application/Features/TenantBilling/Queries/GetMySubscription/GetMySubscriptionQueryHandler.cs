using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.TenantBilling.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.TenantBilling.Queries.GetMySubscription;

public class GetMySubscriptionQueryHandler : IRequestHandler<GetMySubscriptionQuery, MySubscriptionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ITenantUsageCalculator _usageCalculator;
    private readonly IDateTimeService _dateTimeService;

    public GetMySubscriptionQueryHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ITenantUsageCalculator usageCalculator,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _usageCalculator = usageCalculator;
        _dateTimeService = dateTimeService;
    }

    public async Task<MySubscriptionDto> Handle(GetMySubscriptionQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;

        var usage = await _usageCalculator.CalculateAsync(tenantId, cancellationToken);

        // Prefer the active subscription; otherwise show the latest (e.g. pending) one.
        var subscription = await _context.TenantSubscriptions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.Status == TenantSubscriptionStatus.Active)
            .ThenByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var dto = new MySubscriptionDto
        {
            Members = new UsageLineDto { Used = usage.Members },
            Coaches = new UsageLineDto { Used = usage.Coaches },
            Branches = new UsageLineDto { Used = usage.Branches },
            Employees = new UsageLineDto { Used = usage.Employees }
        };

        if (subscription == null)
        {
            dto.HasSubscription = false;
            return dto;
        }

        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == subscription.PlanId, cancellationToken);

        dto.HasSubscription = true;
        dto.SubscriptionId = subscription.Id;
        dto.PlanId = subscription.PlanId;
        dto.PlanName = plan?.Name;
        dto.Status = subscription.Status;
        dto.StartDate = subscription.StartDate;
        dto.EndDate = subscription.EndDate;
        dto.TrialEndsAt = subscription.TrialEndsAt;
        dto.Amount = subscription.Amount;
        dto.Currency = subscription.Currency;
        dto.AutoRenew = subscription.AutoRenew;
        dto.RemainingDays = subscription.EndDate.HasValue
            ? Math.Max(0, (int)Math.Ceiling((subscription.EndDate.Value - now).TotalDays))
            : null;

        if (plan != null)
        {
            dto.Members.Limit = plan.MaxMembers;
            dto.Coaches.Limit = plan.MaxCoaches;
            dto.Branches.Limit = plan.MaxBranches;
            dto.Employees.Limit = plan.MaxEmployees;

            dto.Features = await _context.PlanFeatures
                .Where(pf => pf.PlanId == plan.Id)
                .Select(pf => pf.Feature.Code)
                .ToListAsync(cancellationToken);
        }

        return dto;
    }
}

using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Subscriptions.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Queries.GetSubscriptionPlanById;

public class GetSubscriptionPlanByIdQueryHandler : IRequestHandler<GetSubscriptionPlanByIdQuery, SubscriptionPlanDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetSubscriptionPlanByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<SubscriptionPlanDto?> Handle(GetSubscriptionPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var plan = await _context.SubscriptionPlans
            .Include(p => p.Subscriptions)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId, cancellationToken);

        if (plan == null) return null;

        List<string> features = new();
        if (!string.IsNullOrEmpty(plan.Features))
        {
            try { features = JsonSerializer.Deserialize<List<string>>(plan.Features) ?? new(); }
            catch { }
        }

        return new SubscriptionPlanDto
        {
            Id = plan.Id,
            TenantId = plan.TenantId,
            Name = plan.Name,
            Price = plan.Price,
            DurationMonths = plan.DurationMonths,
            Description = plan.Description,
            Features = features,
            MaxFreezeDays = plan.MaxFreezeDays,
            MaxFreezeCount = plan.MaxFreezeCount,
            IsActive = plan.IsActive,
            SessionsPerWeek = plan.SessionsPerWeek,
            InBodyIncluded = plan.InBodyIncluded,
            PrivateCoach = plan.PrivateCoach,
            ActiveSubscribersCount = plan.Subscriptions.Count(s => s.Status == SubscriptionStatus.Active && !s.IsDeleted)
        };
    }
}

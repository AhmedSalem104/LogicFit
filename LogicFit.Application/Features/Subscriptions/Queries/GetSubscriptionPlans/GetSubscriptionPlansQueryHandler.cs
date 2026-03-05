using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Subscriptions.DTOs;
using LogicFit.Domain.Enums;
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

        var query = _context.SubscriptionPlans
            .Include(p => p.Subscriptions)
            .Where(p => p.TenantId == tenantId)
            .AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(p => p.IsActive == request.IsActive.Value);

        var plans = await query.ToListAsync(cancellationToken);

        return plans.Select(p => new SubscriptionPlanDto
        {
            Id = p.Id,
            TenantId = p.TenantId,
            Name = p.Name,
            Price = p.Price,
            DurationMonths = p.DurationMonths,
            Description = p.Description,
            Features = DeserializeFeatures(p.Features),
            MaxFreezeDays = p.MaxFreezeDays,
            MaxFreezeCount = p.MaxFreezeCount,
            IsActive = p.IsActive,
            SessionsPerWeek = p.SessionsPerWeek,
            InBodyIncluded = p.InBodyIncluded,
            PrivateCoach = p.PrivateCoach,
            ActiveSubscribersCount = p.Subscriptions.Count(s => s.Status == SubscriptionStatus.Active && !s.IsDeleted)
        }).ToList();
    }

    private static List<string> DeserializeFeatures(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
        catch { return new(); }
    }
}

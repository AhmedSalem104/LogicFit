using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.MealLogs.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.MealLogs.Queries.GetNutritionSummary;

public class GetNutritionSummaryQueryHandler : IRequestHandler<GetNutritionSummaryQuery, NutritionSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public GetNutritionSummaryQueryHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<NutritionSummaryDto> Handle(GetNutritionSummaryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var clientId = Guid.Parse(_currentUserService.UserId!);

        var day = (request.Date ?? _dateTimeService.UtcNow).Date;
        var next = day.AddDays(1);

        var logs = await _context.MealLogs
            .Include(l => l.MealItem).ThenInclude(mi => mi.Food)
            .Include(l => l.AlternativeFood)
            .Where(l => l.TenantId == tenantId && l.ClientId == clientId
                        && l.ConsumedAt >= day && l.ConsumedAt < next)
            .ToListAsync(cancellationToken);

        var dtos = logs.Select(MealLogMacros.ToDto).ToList();

        // Targets from the client's diet plan that is active on this day (latest-starting one).
        var plan = await _context.DietPlans
            .Where(p => p.TenantId == tenantId && p.ClientId == clientId
                        && p.StartDate <= next && (p.EndDate == null || p.EndDate >= day))
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        return new NutritionSummaryDto
        {
            Date = day,
            LoggedCount = dtos.Count,
            ConsumedCalories = Math.Round(dtos.Sum(d => d.Calories), 1),
            ConsumedProtein = Math.Round(dtos.Sum(d => d.Protein), 1),
            ConsumedCarbs = Math.Round(dtos.Sum(d => d.Carbs), 1),
            ConsumedFats = Math.Round(dtos.Sum(d => d.Fats), 1),
            TargetCalories = plan?.TargetCalories ?? 0,
            TargetProtein = plan?.TargetProtein ?? 0,
            TargetCarbs = plan?.TargetCarbs ?? 0,
            TargetFats = plan?.TargetFats ?? 0
        };
    }
}

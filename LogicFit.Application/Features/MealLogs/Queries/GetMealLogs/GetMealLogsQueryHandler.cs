using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.MealLogs.DTOs;
using LogicFit.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.MealLogs.Queries.GetMealLogs;

public class GetMealLogsQueryHandler : IRequestHandler<GetMealLogsQuery, List<MealLogDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public GetMealLogsQueryHandler(
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

    public async Task<List<MealLogDto>> Handle(GetMealLogsQuery request, CancellationToken cancellationToken)
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
            .OrderBy(l => l.ConsumedAt)
            .ToListAsync(cancellationToken);

        return logs.Select(MealLogMacros.ToDto).ToList();
    }
}

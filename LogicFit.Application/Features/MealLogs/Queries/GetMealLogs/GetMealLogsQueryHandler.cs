using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.MealLogs.DTOs;
using LogicFit.Domain.Enums;
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
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            throw new LogicFit.Domain.Exceptions.ForbiddenException("An authenticated client is required.");

        var targetClientId = request.ClientId ?? currentUserId;
        var role = await _context.Users.Where(u => u.Id == currentUserId && u.TenantId == tenantId && u.IsActive)
            .Select(u => (UserRole?)u.Role).FirstOrDefaultAsync(cancellationToken)
            ?? throw new LogicFit.Domain.Exceptions.ForbiddenException("The authenticated user is not active in this workspace.");
        if (role == UserRole.Client && targetClientId != currentUserId)
            throw new LogicFit.Domain.Exceptions.ForbiddenException("Clients can only view their own meal logs.");
        if (role is UserRole.Coach or UserRole.Trainer or UserRole.FreelanceCoach)
        {
            if (!await _context.CoachClients.AnyAsync(x => x.TenantId == tenantId && x.CoachId == currentUserId && x.ClientId == targetClientId && x.IsActive && x.UnassignedAt == null, cancellationToken))
                throw new LogicFit.Domain.Exceptions.ForbiddenException("The client is not actively assigned to the current coach.");
        }
        else if (role is not (UserRole.Client or UserRole.Owner or UserRole.Manager or UserRole.FreelanceOwner))
            throw new LogicFit.Domain.Exceptions.ForbiddenException("You cannot view meal logs.");

        // Keep historical logs visible after a coach archives a meal item or food.
        // TenantId and ClientId remain explicit filters, so tenant isolation is preserved.
        var logs = await _context.MealLogs
            .IgnoreQueryFilters()
            .Include(l => l.MealItem).ThenInclude(mi => mi.Food)
            .Include(l => l.MealItem).ThenInclude(mi => mi.Meal)
            .Include(l => l.AlternativeFood)
            .Where(l => l.TenantId == tenantId && l.ClientId == targetClientId)
            .Where(l => request.AllDates || (l.ConsumedAt >= (request.Date ?? _dateTimeService.UtcNow).Date
                        && l.ConsumedAt < (request.Date ?? _dateTimeService.UtcNow).Date.AddDays(1)))
            .OrderBy(l => l.ConsumedAt)
            .ToListAsync(cancellationToken);

        return logs.Select(MealLogMacros.ToDto).ToList();
    }
}

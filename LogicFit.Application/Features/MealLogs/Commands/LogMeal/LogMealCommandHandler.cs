using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.MealLogs.Commands.LogMeal;

public class LogMealCommandHandler : IRequestHandler<LogMealCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public LogMealCommandHandler(
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

    public async Task<Guid> Handle(LogMealCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var clientId = Guid.Parse(_currentUserService.UserId!);

        var mealItem = await _context.MealItems
            .Include(mi => mi.Meal)
            .FirstOrDefaultAsync(mi => mi.Id == request.MealItemId && mi.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("MealItem", request.MealItemId);

        // The meal item must belong to a diet plan owned by the signed-in client.
        var ownsPlan = await _context.DietPlans.AnyAsync(
            p => p.Id == mealItem.Meal.PlanId && p.TenantId == tenantId && p.ClientId == clientId, cancellationToken);
        if (!ownsPlan)
            throw new ForbiddenException("This meal is not part of your diet plan");

        if (request.AlternativeFoodId.HasValue)
        {
            var foodExists = await _context.Foods.AnyAsync(
                f => f.Id == request.AlternativeFoodId.Value && (f.TenantId == tenantId || f.TenantId == null),
                cancellationToken);
            if (!foodExists)
                throw new NotFoundException("Food", request.AlternativeFoodId.Value);
        }

        var log = new MealLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = clientId,
            MealItemId = mealItem.Id,
            ConsumedQuantity = request.ConsumedQuantity,
            ConsumedAt = request.ConsumedAt ?? _dateTimeService.UtcNow,
            AlternativeFoodId = request.AlternativeFoodId
        };

        _context.MealLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        return log.Id;
    }
}

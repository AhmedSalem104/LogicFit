using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.DietPlans.DTOs;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Queries.GetDietPlans;

public class GetDietPlansQueryHandler : IRequestHandler<GetDietPlansQuery, List<DietPlanDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetDietPlansQueryHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<List<DietPlanDto>> Handle(GetDietPlansQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.DietPlans
            .Include(p => p.Coach).ThenInclude(c => c.Profile)
            .Include(p => p.Client).ThenInclude(c => c.Profile)
            .Where(p => p.TenantId == tenantId)
            .AsQueryable();

        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            throw new ForbiddenException("An authenticated workspace user is required.");
        var currentUserRole = await _context.Users
            .Where(u => u.Id == currentUserId && u.TenantId == tenantId && u.IsActive)
            .Select(u => (UserRole?)u.Role)
            .FirstOrDefaultAsync(cancellationToken);
        if (!currentUserRole.HasValue)
            throw new ForbiddenException("The authenticated user is not active in this workspace.");

        if (currentUserRole == UserRole.Client)
        {
            query = query.Where(p => p.ClientId == currentUserId && p.Status == PlanStatus.Active);
        }
        else if (currentUserRole is UserRole.Coach or UserRole.Trainer or UserRole.FreelanceCoach)
        {
            query = query.Where(p => _context.CoachClients.Any(cc => cc.TenantId == tenantId
                && cc.CoachId == currentUserId
                && cc.ClientId == p.ClientId
                && cc.IsActive
                && cc.UnassignedAt == null));
        }

        if (request.CoachId.HasValue)
            query = query.Where(p => p.CoachId == request.CoachId.Value);

        if (request.ClientId.HasValue)
            query = query.Where(p => p.ClientId == request.ClientId.Value);

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        return await query
            .Select(p => new DietPlanDto
            {
                Id = p.Id,
                TenantId = p.TenantId,
                CoachId = p.CoachId,
                CoachName = p.Coach.Profile != null ? p.Coach.Profile.FullName : p.Coach.Email,
                ClientId = p.ClientId,
                ClientName = p.Client.Profile != null ? p.Client.Profile.FullName : p.Client.Email,
                Name = p.Name,
                Description = p.Description,
                MealsPerDay = p.MealsPerDay,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status,
                TargetCalories = p.TargetCalories,
                TargetProtein = p.TargetProtein,
                TargetCarbs = p.TargetCarbs,
                TargetFats = p.TargetFats,
                Meals = p.Meals.Select(m => new DailyMealDto
                {
                    Id = m.Id,
                    PlanId = m.PlanId,
                    Name = m.Name,
                    OrderIndex = m.OrderIndex,
                    Time = m.Time,
                    Items = m.Items.Select(i => new MealItemDto
                    {
                        Id = i.Id,
                        MealId = i.MealId,
                        FoodId = i.FoodId,
                        FoodName = i.Food.Name,
                        AssignedQuantity = i.AssignedQuantity,
                        CalcCalories = i.CalcCalories,
                        CalcProtein = i.CalcProtein,
                        CalcCarbs = i.CalcCarbs,
                        CalcFats = i.CalcFats
                    }).ToList()
                }).ToList()
            })
            .ToListAsync(cancellationToken);
    }
}

using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.ClientDashboard.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ClientDashboard.Queries.GetMyDashboard;

public class GetMyDashboardQueryHandler : IRequestHandler<GetMyDashboardQuery, ClientDashboardDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetMyDashboardQueryHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<ClientDashboardDto> Handle(GetMyDashboardQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var userId = Guid.Parse(_currentUserService.UserId!);

        var programs = await _context.WorkoutPrograms
            .Include(p => p.Coach).ThenInclude(c => c.Profile)
            .Where(p => p.TenantId == tenantId && p.ClientId == userId)
            .OrderByDescending(p => p.StartDate)
            .Take(5)
            .Select(p => new MyWorkoutProgramDto
            {
                Id = p.Id,
                Name = p.Name,
                CoachName = p.Coach.Profile != null ? p.Coach.Profile.FullName : p.Coach.Email,
                StartDate = p.StartDate,
                EndDate = p.EndDate
            })
            .ToListAsync(cancellationToken);

        var dietPlans = await _context.DietPlans
            .Include(d => d.Coach).ThenInclude(c => c.Profile)
            .Where(d => d.TenantId == tenantId && d.ClientId == userId)
            .OrderByDescending(d => d.StartDate)
            .Take(5)
            .Select(d => new MyDietPlanDto
            {
                Id = d.Id,
                Name = d.Name,
                CoachName = d.Coach.Profile != null ? d.Coach.Profile.FullName : d.Coach.Email,
                StartDate = d.StartDate,
                EndDate = d.EndDate,
                Status = d.Status,
                TargetCalories = d.TargetCalories,
                TargetProtein = d.TargetProtein,
                TargetCarbs = d.TargetCarbs,
                TargetFats = d.TargetFats
            })
            .ToListAsync(cancellationToken);

        var activeSubscription = await _context.ClientSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.TenantId == tenantId && s.ClientId == userId && s.Status == SubscriptionStatus.Active)
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
            .FirstOrDefaultAsync(cancellationToken);

        var measurements = await _context.BodyMeasurements
            .Where(m => m.TenantId == tenantId && m.ClientId == userId)
            .OrderByDescending(m => m.DateRecorded)
            .Take(3)
            .Select(m => new MyBodyMeasurementDto
            {
                Id = m.Id,
                DateRecorded = m.DateRecorded,
                WeightKg = m.WeightKg,
                BodyFatPercent = m.BodyFatPercent,
                SkeletalMuscleMass = m.SkeletalMuscleMass
            })
            .ToListAsync(cancellationToken);

        var coach = await _context.CoachClients
            .Include(cc => cc.Coach).ThenInclude(c => c.Profile)
            .Where(cc => cc.TenantId == tenantId && cc.ClientId == userId && cc.IsActive)
            .Select(cc => new MyCoachDto
            {
                CoachId = cc.CoachId,
                FullName = cc.Coach.Profile != null ? cc.Coach.Profile.FullName : null,
                Email = cc.Coach.Email,
                PhoneNumber = cc.Coach.PhoneNumber,
                ProfilePictureUrl = cc.Coach.Profile != null ? cc.Coach.Profile.ProfilePictureUrl : null,
                AssignedAt = cc.AssignedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        var unreadCount = await _context.Notifications
            .CountAsync(n => n.RecipientId == userId && !n.IsRead, cancellationToken);

        return new ClientDashboardDto
        {
            ActivePrograms = programs,
            ActiveDietPlans = dietPlans,
            ActiveSubscription = activeSubscription,
            RecentMeasurements = measurements,
            AssignedCoach = coach,
            UnreadNotificationCount = unreadCount
        };
    }
}

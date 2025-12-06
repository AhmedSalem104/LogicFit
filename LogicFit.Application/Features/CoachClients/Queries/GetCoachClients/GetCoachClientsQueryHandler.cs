using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.CoachClients.DTOs;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.CoachClients.Queries.GetCoachClients;

public class GetCoachClientsQueryHandler : IRequestHandler<GetCoachClientsQuery, List<CoachClientDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCoachClientsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<CoachClientDto>> Handle(GetCoachClientsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = Guid.Parse(_currentUserService.UserId!);

        // Get current user to check role
        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken)
            ?? throw new NotFoundException("Current user not found");

        // Determine which coach to query for
        Guid? coachId = null;
        if (request.CoachId.HasValue)
        {
            // Only Owner can view other coaches' clients
            if (currentUser.Role != UserRole.Owner && request.CoachId.Value != currentUserId)
                throw new ForbiddenException("You can only view your own clients");

            coachId = request.CoachId;
        }
        else if (currentUser.Role == UserRole.Coach)
        {
            // Coach viewing their own clients
            coachId = currentUserId;
        }
        // Owner without coachId filter sees all

        var query = _context.CoachClients
            .Include(cc => cc.Coach).ThenInclude(c => c.Profile)
            .Include(cc => cc.Client).ThenInclude(c => c.Profile)
            .Include(cc => cc.Client).ThenInclude(c => c.Subscriptions)
            .Include(cc => cc.Client).ThenInclude(c => c.ClientWorkoutPrograms)
            .Include(cc => cc.Client).ThenInclude(c => c.ClientDietPlans)
            .Include(cc => cc.Client).ThenInclude(c => c.WorkoutSessions)
            .AsQueryable();

        // Filter by coach
        if (coachId.HasValue)
            query = query.Where(cc => cc.CoachId == coachId.Value);

        // Filter by active status
        if (request.IsActive.HasValue)
            query = query.Where(cc => cc.IsActive == request.IsActive.Value);

        var result = await query
            .OrderByDescending(cc => cc.AssignedAt)
            .Select(cc => new CoachClientDto
            {
                Id = cc.Id,
                CoachId = cc.CoachId,
                CoachName = cc.Coach.Profile != null ? cc.Coach.Profile.FullName ?? cc.Coach.Email : cc.Coach.Email,
                ClientId = cc.ClientId,
                ClientName = cc.Client.Profile != null ? cc.Client.Profile.FullName ?? cc.Client.Email : cc.Client.Email,
                ClientPhone = cc.Client.PhoneNumber,
                ClientEmail = cc.Client.Email,
                AssignedAt = cc.AssignedAt,
                UnassignedAt = cc.UnassignedAt,
                IsActive = cc.IsActive,
                Notes = cc.Notes,
                HasActiveSubscription = cc.Client.Subscriptions
                    .Any(s => s.Status == SubscriptionStatus.Active && s.EndDate > DateTime.UtcNow),
                SubscriptionEndDate = cc.Client.Subscriptions
                    .Where(s => s.Status == SubscriptionStatus.Active)
                    .OrderByDescending(s => s.EndDate)
                    .Select(s => (DateTime?)s.EndDate)
                    .FirstOrDefault(),
                WorkoutProgramsCount = cc.Client.ClientWorkoutPrograms.Count(wp => !wp.IsDeleted),
                DietPlansCount = cc.Client.ClientDietPlans.Count(dp => !dp.IsDeleted && dp.Status == PlanStatus.Active),
                WorkoutSessionsCount = cc.Client.WorkoutSessions.Count(ws => !ws.IsDeleted),
                LastSessionDate = cc.Client.WorkoutSessions
                    .Where(ws => !ws.IsDeleted)
                    .OrderByDescending(ws => ws.StartedAt)
                    .Select(ws => (DateTime?)ws.StartedAt)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return result;
    }
}

using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetCoachTraineesReport;

public class GetCoachTraineesReportQueryHandler : IRequestHandler<GetCoachTraineesReportQuery, CoachTraineesReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public GetCoachTraineesReportQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<CoachTraineesReportDto> Handle(GetCoachTraineesReportQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = Guid.Parse(_currentUserService.UserId!);
        var now = _dateTimeService.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Determine coach ID
        Guid coachId;
        if (request.CoachId.HasValue)
        {
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken)
                ?? throw new NotFoundException("Current user not found");

            if (currentUser.Role != UserRole.Owner && request.CoachId.Value != currentUserId)
                throw new ForbiddenException("You can only view your own trainees report");

            coachId = request.CoachId.Value;
        }
        else
        {
            coachId = currentUserId;
        }

        // Get coach-client assignments
        var coachClients = await _context.CoachClients
            .Where(cc => cc.CoachId == coachId && cc.IsActive)
            .Include(cc => cc.Client).ThenInclude(c => c.Profile)
            .Include(cc => cc.Client).ThenInclude(c => c.Subscriptions)
            .Include(cc => cc.Client).ThenInclude(c => c.ClientWorkoutPrograms)
            .Include(cc => cc.Client).ThenInclude(c => c.ClientDietPlans)
            .Include(cc => cc.Client).ThenInclude(c => c.WorkoutSessions)
            .Include(cc => cc.Client).ThenInclude(c => c.BodyMeasurements)
            .OrderByDescending(cc => cc.AssignedAt)
            .ToListAsync(cancellationToken);

        var trainees = new List<TraineeDetailDto>();

        foreach (var cc in coachClients)
        {
            var client = cc.Client;
            var measurements = client.BodyMeasurements
                .Where(m => !m.IsDeleted)
                .OrderByDescending(m => m.DateRecorded)
                .ToList();

            var firstMeasurement = measurements.LastOrDefault();
            var lastMeasurement = measurements.FirstOrDefault();

            var activeSubscription = client.Subscriptions
                .FirstOrDefault(s => s.Status == SubscriptionStatus.Active && s.EndDate > now);

            var sessionsThisMonth = client.WorkoutSessions
                .Count(ws => !ws.IsDeleted && ws.StartedAt >= startOfMonth);

            trainees.Add(new TraineeDetailDto
            {
                ClientId = client.Id,
                Name = client.Profile?.FullName ?? client.Email,
                Phone = client.PhoneNumber,
                Email = client.Email,
                AssignedAt = cc.AssignedAt,
                HasActiveSubscription = activeSubscription != null,
                SubscriptionEndDate = activeSubscription?.EndDate,
                ActiveWorkoutPrograms = client.ClientWorkoutPrograms.Count(wp => !wp.IsDeleted),
                ActiveDietPlans = client.ClientDietPlans.Count(dp => !dp.IsDeleted && dp.Status == PlanStatus.Active),
                TotalSessions = client.WorkoutSessions.Count(ws => !ws.IsDeleted),
                SessionsThisMonth = sessionsThisMonth,
                LastSessionDate = client.WorkoutSessions
                    .Where(ws => !ws.IsDeleted)
                    .OrderByDescending(ws => ws.StartedAt)
                    .FirstOrDefault()?.StartedAt,
                CurrentWeight = lastMeasurement?.WeightKg,
                WeightChange = firstMeasurement != null && lastMeasurement != null
                    ? lastMeasurement.WeightKg - firstMeasurement.WeightKg
                    : null,
                BodyFatPercent = lastMeasurement?.BodyFatPercent,
                LastMeasurementDate = lastMeasurement?.DateRecorded
            });
        }

        var totalTrainees = trainees.Count;
        var withActiveSubscription = trainees.Count(t => t.HasActiveSubscription);
        var withoutSubscription = totalTrainees - withActiveSubscription;

        return new CoachTraineesReportDto
        {
            TotalTrainees = totalTrainees,
            WithActiveSubscription = withActiveSubscription,
            WithoutSubscription = withoutSubscription,
            Trainees = trainees
        };
    }
}

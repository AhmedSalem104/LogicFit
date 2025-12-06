using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetCoachDashboardReport;

public class GetCoachDashboardReportQueryHandler : IRequestHandler<GetCoachDashboardReportQuery, CoachDashboardReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public GetCoachDashboardReportQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<CoachDashboardReportDto> Handle(GetCoachDashboardReportQuery request, CancellationToken cancellationToken)
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
                throw new ForbiddenException("You can only view your own dashboard");

            coachId = request.CoachId.Value;
        }
        else
        {
            coachId = currentUserId;
        }

        // Get trainee IDs for this coach
        var traineeIds = await _context.CoachClients
            .Where(cc => cc.CoachId == coachId && cc.IsActive)
            .Select(cc => cc.ClientId)
            .ToListAsync(cancellationToken);

        var newTraineeIds = await _context.CoachClients
            .Where(cc => cc.CoachId == coachId && cc.IsActive && cc.AssignedAt >= startOfMonth)
            .Select(cc => cc.ClientId)
            .ToListAsync(cancellationToken);

        // Trainee stats
        var totalTrainees = traineeIds.Count;
        var activeTrainees = await _context.Users
            .Where(u => traineeIds.Contains(u.Id) && u.IsActive)
            .CountAsync(cancellationToken);
        var newTraineesThisMonth = newTraineeIds.Count;

        // Program stats
        var activeWorkoutPrograms = await _context.WorkoutPrograms
            .Where(wp => wp.CoachId == coachId && traineeIds.Contains(wp.ClientId))
            .CountAsync(cancellationToken);

        var activeDietPlans = await _context.DietPlans
            .Where(dp => dp.CoachId == coachId && traineeIds.Contains(dp.ClientId) && dp.Status == PlanStatus.Active)
            .CountAsync(cancellationToken);

        // Session stats this month
        var sessionsThisMonth = await _context.WorkoutSessions
            .Where(ws => traineeIds.Contains(ws.ClientId) && ws.StartedAt >= startOfMonth)
            .ToListAsync(cancellationToken);

        var totalSessionsThisMonth = sessionsThisMonth.Count;
        var totalVolumeThisMonth = sessionsThisMonth.Sum(ws => ws.TotalVolumLifted);

        // Top trainees by sessions
        var topTraineesBySessions = await _context.WorkoutSessions
            .Where(ws => traineeIds.Contains(ws.ClientId))
            .GroupBy(ws => ws.ClientId)
            .Select(g => new { ClientId = g.Key, SessionsCount = g.Count() })
            .OrderByDescending(x => x.SessionsCount)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topTraineesBySessionsResult = new List<TopTraineeDto>();
        foreach (var t in topTraineesBySessions)
        {
            var client = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == t.ClientId, cancellationToken);

            if (client != null)
            {
                topTraineesBySessionsResult.Add(new TopTraineeDto
                {
                    ClientId = client.Id,
                    ClientName = client.Profile?.FullName ?? client.Email,
                    ClientPhone = client.PhoneNumber,
                    SessionsCount = t.SessionsCount
                });
            }
        }

        // Top trainees by progress (weight change)
        var topTraineesByProgress = new List<TopTraineeDto>();
        foreach (var traineeId in traineeIds.Take(10))
        {
            var measurements = await _context.BodyMeasurements
                .Where(bm => bm.ClientId == traineeId)
                .OrderBy(bm => bm.DateRecorded)
                .ToListAsync(cancellationToken);

            if (measurements.Count >= 2)
            {
                var first = measurements.First();
                var last = measurements.Last();
                var weightChange = last.WeightKg - first.WeightKg;
                var bodyFatChange = (last.BodyFatPercent ?? 0) - (first.BodyFatPercent ?? 0);

                var client = await _context.Users
                    .Include(u => u.Profile)
                    .FirstOrDefaultAsync(u => u.Id == traineeId, cancellationToken);

                if (client != null)
                {
                    topTraineesByProgress.Add(new TopTraineeDto
                    {
                        ClientId = client.Id,
                        ClientName = client.Profile?.FullName ?? client.Email,
                        ClientPhone = client.PhoneNumber,
                        WeightChange = weightChange,
                        BodyFatChange = bodyFatChange
                    });
                }
            }
        }

        topTraineesByProgress = topTraineesByProgress
            .OrderBy(t => t.WeightChange)  // Assuming weight loss is progress
            .Take(5)
            .ToList();

        return new CoachDashboardReportDto
        {
            TotalTrainees = totalTrainees,
            ActiveTrainees = activeTrainees,
            NewTraineesThisMonth = newTraineesThisMonth,
            ActiveWorkoutPrograms = activeWorkoutPrograms,
            ActiveDietPlans = activeDietPlans,
            TotalSessionsThisMonth = totalSessionsThisMonth,
            TotalVolumeThisMonth = totalVolumeThisMonth,
            TopTraineesBySessions = topTraineesBySessionsResult,
            TopTraineesByProgress = topTraineesByProgress
        };
    }
}

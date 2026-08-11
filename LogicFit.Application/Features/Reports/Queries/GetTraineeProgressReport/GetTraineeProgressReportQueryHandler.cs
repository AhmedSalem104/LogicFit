using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetTraineeProgressReport;

public class GetTraineeProgressReportQueryHandler : IRequestHandler<GetTraineeProgressReportQuery, TraineeProgressReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetTraineeProgressReportQueryHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<TraineeProgressReportDto> Handle(GetTraineeProgressReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            throw new ForbiddenException("An authenticated workspace user is required.");

        // Verify the current user is the coach of this client or owner
        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId && u.TenantId == tenantId && u.IsActive, cancellationToken)
            ?? throw new NotFoundException("Current user not found");

        var coachClient = await _context.CoachClients
            .FirstOrDefaultAsync(cc => cc.TenantId == tenantId
                && cc.ClientId == request.ClientId && cc.IsActive, cancellationToken);

        if (currentUser.Role == UserRole.Client && request.ClientId != currentUserId)
            throw new ForbiddenException("Clients can only view their own progress.");

        if (currentUser.Role is (UserRole.Coach or UserRole.Trainer or UserRole.FreelanceCoach)
            && (coachClient == null || coachClient.CoachId != currentUserId))
            throw new ForbiddenException("This client is not assigned to you");

        // Get client details
        var client = await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == request.ClientId
                && u.TenantId == tenantId
                && u.Role == UserRole.Client
                && u.IsActive, cancellationToken)
            ?? throw new NotFoundException("Client not found");

        // Get body measurements
        var measurements = await _context.BodyMeasurements
            .Where(bm => bm.TenantId == tenantId && bm.ClientId == request.ClientId)
            .OrderBy(bm => bm.DateRecorded)
            .ToListAsync(cancellationToken);

        var firstMeasurement = measurements.FirstOrDefault();
        var lastMeasurement = measurements.LastOrDefault();

        var bodyProgress = measurements.Select(m => new BodyProgressDto
        {
            DateRecorded = m.DateRecorded,
            WeightKg = m.WeightKg,
            BodyFatPercent = m.BodyFatPercent,
            MuscleMass = m.SkeletalMuscleMass,
            Bmr = m.Bmr
        }).ToList();

        // Get workout sessions
        var sessions = await _context.WorkoutSessions
            .Where(ws => ws.TenantId == tenantId && ws.ClientId == request.ClientId)
            .ToListAsync(cancellationToken);

        var totalSessions = sessions.Count;
        var totalVolumeLifted = sessions.Sum(s => s.TotalVolumLifted);

        // Monthly sessions breakdown (last 6 months)
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var monthlySessions = sessions
            .Where(s => s.StartedAt >= sixMonthsAgo)
            .GroupBy(s => new { s.StartedAt.Year, s.StartedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlySessionsDto
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                SessionCount = g.Count(),
                TotalVolume = g.Sum(s => s.TotalVolumLifted)
            })
            .ToList();

        // Get active workout programs
        var workoutPrograms = await _context.WorkoutPrograms
            .Where(wp => wp.TenantId == tenantId && wp.ClientId == request.ClientId && !wp.IsDeleted)
            .Include(wp => wp.Routines)
            .Select(wp => new ActiveProgramDto
            {
                Id = wp.Id,
                Name = wp.Name,
                StartDate = wp.StartDate,
                RoutinesCount = wp.Routines.Count(r => !r.IsDeleted)
            })
            .ToListAsync(cancellationToken);

        // Get active diet plans
        var dietPlans = await _context.DietPlans
            .Where(dp => dp.TenantId == tenantId && dp.ClientId == request.ClientId
                && dp.Status == PlanStatus.Active && !dp.IsDeleted)
            .Select(dp => new ActivePlanDto
            {
                Id = dp.Id,
                Name = dp.Name,
                StartDate = dp.StartDate,
                TargetCalories = dp.TargetCalories
            })
            .ToListAsync(cancellationToken);

        // Get personal records (best lifts)
        var personalRecords = await _context.SessionSets
            .Include(ss => ss.Session)
            .Include(ss => ss.Exercise)
            .Where(ss => ss.Session.TenantId == tenantId
                && ss.Session.ClientId == request.ClientId && ss.IsPr)
            .OrderByDescending(ss => ss.WeightKg)
            .Take(10)
            .Select(ss => new PersonalRecordDto
            {
                ExerciseId = ss.ExerciseId,
                ExerciseName = ss.Exercise.Name,
                MaxWeight = ss.WeightKg,
                Reps = ss.Reps,
                AchievedAt = ss.Session.StartedAt
            })
            .ToListAsync(cancellationToken);

        return new TraineeProgressReportDto
        {
            ClientId = client.Id,
            ClientName = client.Profile?.FullName ?? client.Email,
            ClientPhone = client.PhoneNumber,
            AssignedAt = coachClient?.AssignedAt ?? DateTime.UtcNow,

            // Body Progress
            BodyMeasurements = bodyProgress,
            StartWeight = firstMeasurement?.WeightKg,
            CurrentWeight = lastMeasurement?.WeightKg,
            TotalWeightChange = firstMeasurement != null && lastMeasurement != null
                ? lastMeasurement.WeightKg - firstMeasurement.WeightKg
                : null,
            StartBodyFat = firstMeasurement?.BodyFatPercent,
            CurrentBodyFat = lastMeasurement?.BodyFatPercent,
            TotalBodyFatChange = firstMeasurement?.BodyFatPercent != null && lastMeasurement?.BodyFatPercent != null
                ? lastMeasurement.BodyFatPercent - firstMeasurement.BodyFatPercent
                : null,
            StartMuscleMass = firstMeasurement?.SkeletalMuscleMass,
            CurrentMuscleMass = lastMeasurement?.SkeletalMuscleMass,
            TotalMuscleMassChange = firstMeasurement?.SkeletalMuscleMass != null && lastMeasurement?.SkeletalMuscleMass != null
                ? lastMeasurement.SkeletalMuscleMass - firstMeasurement.SkeletalMuscleMass
                : null,

            // Workout Progress
            TotalSessions = totalSessions,
            TotalVolumeLifted = totalVolumeLifted,
            MonthlySessions = monthlySessions,

            // Active Programs
            WorkoutPrograms = workoutPrograms,
            DietPlans = dietPlans,

            // Personal Records
            PersonalRecords = personalRecords
        };
    }
}

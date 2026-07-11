using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ClassSchedules.Commands.CreateClassSchedule;

public class CreateClassScheduleCommandHandler : IRequestHandler<CreateClassScheduleCommand, Guid>
{
    private const int MaxOccurrences = 366; // safety cap for a recurring series

    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateClassScheduleCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateClassScheduleCommand request, CancellationToken cancellationToken)
    {
        if (request.EndTime <= request.StartTime)
            throw new DomainException("EndTime must be after StartTime");

        var tenantId = _tenantService.GetCurrentTenantId();

        var groupClassExists = await _context.GroupClasses
            .AnyAsync(g => g.Id == request.GroupClassId && g.TenantId == tenantId, cancellationToken);
        if (!groupClassExists)
            throw new NotFoundException("GroupClass", request.GroupClassId);

        // Expand the recurrence into concrete, individually-bookable sessions. A non-recurring request
        // yields exactly one session (identical to the original behaviour).
        var duration = request.EndTime - request.StartTime;
        var occurrences = BuildOccurrences(request, duration);

        // Load existing non-cancelled schedules for this coach/room within the series window once, then
        // check each occurrence in memory so a recurring create is a single pair of queries, not N.
        var windowEnd = occurrences[^1].End;
        var existingRoom = request.RoomId.HasValue
            ? await _context.ClassSchedules
                .Where(s => s.TenantId == tenantId && s.RoomId == request.RoomId.Value && !s.IsCancelled
                            && s.StartTime < windowEnd && s.EndTime > request.StartTime)
                .Select(s => new { s.StartTime, s.EndTime })
                .ToListAsync(cancellationToken)
            : new();
        var existingCoach = request.CoachId.HasValue
            ? await _context.ClassSchedules
                .Where(s => s.TenantId == tenantId && s.CoachId == request.CoachId.Value && !s.IsCancelled
                            && s.StartTime < windowEnd && s.EndTime > request.StartTime)
                .Select(s => new { s.StartTime, s.EndTime })
                .ToListAsync(cancellationToken)
            : new();

        var schedules = new List<ClassSchedule>();
        foreach (var (start, end) in occurrences)
        {
            if (request.RoomId.HasValue && existingRoom.Any(e => e.StartTime < end && e.EndTime > start))
                throw new ConflictException($"Room is already booked at {start:yyyy-MM-dd HH:mm}");
            if (request.CoachId.HasValue && existingCoach.Any(e => e.StartTime < end && e.EndTime > start))
                throw new ConflictException($"Coach already has a class at {start:yyyy-MM-dd HH:mm}");

            schedules.Add(new ClassSchedule
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                GroupClassId = request.GroupClassId,
                CoachId = request.CoachId,
                RoomId = request.RoomId,
                StartTime = start,
                EndTime = end,
                // Each generated row is a concrete session; the recurrence metadata is kept on the first
                // (series anchor) row only, so callers can still see how the series was defined.
                RecurrencePattern = schedules.Count == 0 ? request.RecurrencePattern : RecurrencePattern.None,
                RecurrenceDaysOfWeek = schedules.Count == 0 ? request.RecurrenceDaysOfWeek : null,
                RecurrenceEndDate = schedules.Count == 0 ? request.RecurrenceEndDate : null,
                OverrideCapacity = request.OverrideCapacity
            });
        }

        _context.ClassSchedules.AddRange(schedules);
        await _context.SaveChangesAsync(cancellationToken);

        // Return the first (anchor) session's id.
        return schedules[0].Id;
    }

    private static List<(DateTime Start, DateTime End)> BuildOccurrences(CreateClassScheduleCommand request, TimeSpan duration)
    {
        // No recurrence, or missing an end date to bound the series -> a single session.
        if (request.RecurrencePattern == RecurrencePattern.None || request.RecurrenceEndDate == null)
            return new() { (request.StartTime, request.EndTime) };

        var result = new List<(DateTime, DateTime)>();
        var timeOfDay = request.StartTime.TimeOfDay;
        var seriesEnd = request.RecurrenceEndDate.Value.Date;

        void Add(DateTime day)
        {
            var start = day.Date + timeOfDay;
            if (start >= request.StartTime.AddSeconds(-1) && result.Count < MaxOccurrences)
                result.Add((start, start + duration));
        }

        if (request.RecurrencePattern == RecurrencePattern.Weekly && !string.IsNullOrWhiteSpace(request.RecurrenceDaysOfWeek))
        {
            var wanted = ParseDaysOfWeek(request.RecurrenceDaysOfWeek);
            for (var d = request.StartTime.Date; d <= seriesEnd && result.Count < MaxOccurrences; d = d.AddDays(1))
                if (wanted.Contains(d.DayOfWeek))
                    Add(d);
        }
        else
        {
            for (var d = request.StartTime.Date; d.Date <= seriesEnd && result.Count < MaxOccurrences;)
            {
                Add(d);
                d = request.RecurrencePattern switch
                {
                    RecurrencePattern.Daily => d.AddDays(1),
                    RecurrencePattern.Weekly => d.AddDays(7),
                    RecurrencePattern.Monthly => d.AddMonths(1),
                    _ => seriesEnd.AddDays(1)
                };
            }
        }

        // Always include at least the anchor session.
        if (result.Count == 0)
            result.Add((request.StartTime, request.EndTime));

        return result;
    }

    private static HashSet<DayOfWeek> ParseDaysOfWeek(string csv)
    {
        var days = new HashSet<DayOfWeek>();
        foreach (var raw in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            DayOfWeek? day = raw.ToLowerInvariant() switch
            {
                "sun" or "sunday" => DayOfWeek.Sunday,
                "mon" or "monday" => DayOfWeek.Monday,
                "tue" or "tues" or "tuesday" => DayOfWeek.Tuesday,
                "wed" or "weds" or "wednesday" => DayOfWeek.Wednesday,
                "thu" or "thur" or "thurs" or "thursday" => DayOfWeek.Thursday,
                "fri" or "friday" => DayOfWeek.Friday,
                "sat" or "saturday" => DayOfWeek.Saturday,
                _ => null
            };
            if (day.HasValue) days.Add(day.Value);
        }
        return days;
    }
}

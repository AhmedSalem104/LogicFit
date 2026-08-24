using LogicFit.Application.Features.AthleteCheckins.DTOs;
using LogicFit.Application.Features.BodyMeasurements.DTOs;
using LogicFit.Application.Features.Clients.DTOs;
using LogicFit.Application.Features.DietPlans.DTOs;
using LogicFit.Application.Features.MealLogs.DTOs;
using LogicFit.Application.Features.Subscriptions.DTOs;
using LogicFit.Application.Features.WorkoutPrograms.DTOs;
using LogicFit.Application.Features.WorkoutSessions.DTOs;

namespace LogicFit.Application.Features.Clients.DTOs;

public sealed class ClientTrainingOverviewDto
{
    public ClientDto Client { get; set; } = null!;
    public List<ClientSubscriptionDto> Subscriptions { get; set; } = new();
    public List<WorkoutProgramDto> WorkoutPrograms { get; set; } = new();
    public List<DietPlanDto> DietPlans { get; set; } = new();
    public List<BodyMeasurementDto> Measurements { get; set; } = new();
    public List<AthleteCheckinDto> Checkins { get; set; } = new();
    public List<WorkoutSessionDto> WorkoutSessions { get; set; } = new();
    public List<MealLogDto> MealLogs { get; set; } = new();
    public int CompletedWorkoutSessions => WorkoutSessions.Count(x => x.EndedAt.HasValue);
    public DateTime? LastMeasurementAt => Measurements.OrderByDescending(x => x.DateRecorded).FirstOrDefault()?.DateRecorded;
    public DateTime? LastCheckinAt => Checkins.OrderByDescending(x => x.CheckinDate).FirstOrDefault()?.CheckinDate;
    public DateTime? LastActivityAt => new[]
    {
        LastMeasurementAt,
        LastCheckinAt,
        WorkoutSessions.OrderByDescending(x => x.StartedAt).FirstOrDefault()?.StartedAt,
        MealLogs.OrderByDescending(x => x.ConsumedAt).FirstOrDefault()?.ConsumedAt
    }.Where(x => x.HasValue).Max();
}

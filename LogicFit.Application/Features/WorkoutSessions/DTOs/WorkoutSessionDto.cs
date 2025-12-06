namespace LogicFit.Application.Features.WorkoutSessions.DTOs;

public class WorkoutSessionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public string? ClientName { get; set; }
    public Guid RoutineId { get; set; }
    public string? RoutineName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public double TotalVolumLifted { get; set; }
    public string? Notes { get; set; }
    public List<SessionSetDto> Sets { get; set; } = new();
}

public class SessionSetDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public int ExerciseId { get; set; }
    public string? ExerciseName { get; set; }
    public int SetNumber { get; set; }
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public double? Rpe { get; set; }
    public double VolumeLoad { get; set; }
    public bool IsPr { get; set; }
}

public class StartWorkoutSessionDto
{
    public Guid RoutineId { get; set; }
}

public class CreateSessionSetDto
{
    public Guid SessionId { get; set; }
    public int ExerciseId { get; set; }
    public int SetNumber { get; set; }
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public double? Rpe { get; set; }
}

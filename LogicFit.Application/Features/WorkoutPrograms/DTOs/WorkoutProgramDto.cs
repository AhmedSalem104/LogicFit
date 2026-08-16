namespace LogicFit.Application.Features.WorkoutPrograms.DTOs;

using LogicFit.Domain.Enums;

public class WorkoutProgramDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CoachId { get; set; }
    public string? CoachName { get; set; }
    public Guid ClientId { get; set; }
    public string? ClientName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Goal { get; set; }
    public string? Difficulty { get; set; }
    public int? DaysPerWeek { get; set; }
    public PlanStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
    public int Version { get; set; }
    public List<ProgramRoutineDto> Routines { get; set; } = new();
}

public class ProgramRoutineDto
{
    public Guid Id { get; set; }
    public Guid ProgramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public string? Notes { get; set; }
    public List<RoutineExerciseDto> Exercises { get; set; } = new();
}

public class RoutineExerciseDto
{
    public Guid Id { get; set; }
    public Guid RoutineId { get; set; }
    public int ExerciseId { get; set; }
    public string? ExerciseName { get; set; }
    public int Sets { get; set; }
    public int RepsMin { get; set; }
    public int RepsMax { get; set; }
    public int RestSec { get; set; }
    public double? TargetWeightKg { get; set; }
    public string? Notes { get; set; }
    public string? Tempo { get; set; }
    public Guid? SupersetGroupId { get; set; }
}

public class CreateWorkoutProgramDto
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }
    public string? Goal { get; set; }
    public string? Difficulty { get; set; }
    public int? DaysPerWeek { get; set; }
    public PlanStatus? Status { get; set; }
    public List<WorkoutRoutineInputDto> Routines { get; set; } = new();
}

public class WorkoutRoutineInputDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public string? Notes { get; set; }
    public List<WorkoutRoutineExerciseInputDto> Exercises { get; set; } = new();
}

public class WorkoutRoutineExerciseInputDto
{
    public Guid? Id { get; set; }
    public int ExerciseId { get; set; }
    public int Sets { get; set; }
    public int RepsMin { get; set; }
    public int RepsMax { get; set; }
    public int RestSec { get; set; }
    public double? TargetWeightKg { get; set; }
    public string? Notes { get; set; }
    public string? Tempo { get; set; }
    public Guid? SupersetGroupId { get; set; }
}

public class CreateProgramRoutineDto
{
    public Guid ProgramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
}

public class CreateRoutineExerciseDto
{
    public Guid RoutineId { get; set; }
    public int ExerciseId { get; set; }
    public int Sets { get; set; }
    public int RepsMin { get; set; }
    public int RepsMax { get; set; }
    public int RestSec { get; set; }
    public double? TargetWeightKg { get; set; }
    public string? Notes { get; set; }
    public string? Tempo { get; set; }
    public Guid? SupersetGroupId { get; set; }
}

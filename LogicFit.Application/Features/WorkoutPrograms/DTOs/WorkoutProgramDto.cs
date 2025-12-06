namespace LogicFit.Application.Features.WorkoutPrograms.DTOs;

public class WorkoutProgramDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CoachId { get; set; }
    public string? CoachName { get; set; }
    public Guid ClientId { get; set; }
    public string? ClientName { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<ProgramRoutineDto> Routines { get; set; } = new();
}

public class ProgramRoutineDto
{
    public Guid Id { get; set; }
    public Guid ProgramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
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
    public Guid? SupersetGroupId { get; set; }
}

public class CreateWorkoutProgramDto
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
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
    public Guid? SupersetGroupId { get; set; }
}

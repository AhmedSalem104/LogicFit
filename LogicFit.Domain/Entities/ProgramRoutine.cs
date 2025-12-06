using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class ProgramRoutine : TenantAuditableEntity
{
    public Guid ProgramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }

    // Navigation Properties
    public virtual WorkoutProgram Program { get; set; } = null!;
    public virtual ICollection<RoutineExercise> Exercises { get; set; } = new List<RoutineExercise>();
    public virtual ICollection<WorkoutSession> Sessions { get; set; } = new List<WorkoutSession>();
}

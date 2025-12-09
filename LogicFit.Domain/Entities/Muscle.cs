using LogicFit.Domain.Common.Interfaces;

namespace LogicFit.Domain.Entities;

public class Muscle : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BodyPart { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation Properties
    public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
    public virtual ICollection<ExerciseSecondaryMuscle> SecondaryExercises { get; set; } = new List<ExerciseSecondaryMuscle>();
}

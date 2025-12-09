using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class ExerciseSecondaryMuscleConfiguration : IEntityTypeConfiguration<ExerciseSecondaryMuscle>
{
    public void Configure(EntityTypeBuilder<ExerciseSecondaryMuscle> builder)
    {
        builder.ToTable("ExerciseSecondaryMuscles");

        // Composite primary key
        builder.HasKey(e => new { e.ExerciseId, e.MuscleId });

        builder.Property(e => e.ContributionPercent)
            .IsRequired();

        // Index for efficient muscle lookups
        builder.HasIndex(e => e.MuscleId);

        // Relationships
        builder.HasOne(e => e.Exercise)
            .WithMany(ex => ex.SecondaryMuscles)
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Muscle)
            .WithMany(m => m.SecondaryExercises)
            .HasForeignKey(e => e.MuscleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

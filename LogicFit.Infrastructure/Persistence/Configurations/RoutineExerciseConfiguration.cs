using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class RoutineExerciseConfiguration : IEntityTypeConfiguration<RoutineExercise>
{
    public void Configure(EntityTypeBuilder<RoutineExercise> builder)
    {
        builder.ToTable("RoutineExercises");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.RoutineId);
        builder.HasIndex(e => e.SupersetGroupId);

        builder.HasOne(e => e.Routine)
            .WithMany(r => r.Exercises)
            .HasForeignKey(e => e.RoutineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Exercise)
            .WithMany(ex => ex.RoutineExercises)
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

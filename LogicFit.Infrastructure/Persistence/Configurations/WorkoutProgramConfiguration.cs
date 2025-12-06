using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class WorkoutProgramConfiguration : IEntityTypeConfiguration<WorkoutProgram>
{
    public void Configure(EntityTypeBuilder<WorkoutProgram> builder)
    {
        builder.ToTable("WorkoutPrograms");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.CoachId);
        builder.HasIndex(e => e.ClientId);

        builder.HasOne(e => e.Coach)
            .WithMany(u => u.CoachWorkoutPrograms)
            .HasForeignKey(e => e.CoachId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Client)
            .WithMany(u => u.ClientWorkoutPrograms)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

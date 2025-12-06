using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("Exercises");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.ImageUrl)
            .HasMaxLength(500);

        builder.Property(e => e.VideoUrl)
            .HasMaxLength(500);

        builder.Property(e => e.Equipment)
            .HasMaxLength(100);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.TargetMuscleId);

        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.Exercises)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TargetMuscle)
            .WithMany(m => m.Exercises)
            .HasForeignKey(e => e.TargetMuscleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

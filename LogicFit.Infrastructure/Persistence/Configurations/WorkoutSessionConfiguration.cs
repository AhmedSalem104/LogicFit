using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class WorkoutSessionConfiguration : IEntityTypeConfiguration<WorkoutSession>
{
    public void Configure(EntityTypeBuilder<WorkoutSession> builder)
    {
        builder.ToTable("WorkoutSessions");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.ClientId);
        builder.HasIndex(e => e.RoutineId);
        builder.HasIndex(e => e.StartedAt);

        builder.HasOne(e => e.Client)
            .WithMany(u => u.WorkoutSessions)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Routine)
            .WithMany(r => r.Sessions)
            .HasForeignKey(e => e.RoutineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

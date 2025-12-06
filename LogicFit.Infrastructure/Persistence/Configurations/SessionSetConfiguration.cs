using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class SessionSetConfiguration : IEntityTypeConfiguration<SessionSet>
{
    public void Configure(EntityTypeBuilder<SessionSet> builder)
    {
        builder.ToTable("SessionSets");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.WeightKg)
            .HasPrecision(10, 2);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.SessionId);

        builder.HasOne(e => e.Session)
            .WithMany(s => s.Sets)
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Exercise)
            .WithMany(ex => ex.SessionSets)
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

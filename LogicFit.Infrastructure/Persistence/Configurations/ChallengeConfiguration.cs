using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class ChallengeConfiguration : IEntityTypeConfiguration<Challenge>
{
    public void Configure(EntityTypeBuilder<Challenge> builder)
    {
        builder.ToTable("Challenges");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.TargetMetric).HasMaxLength(100);

        builder.HasOne(e => e.CreatedByCoach)
            .WithMany()
            .HasForeignKey(e => e.CreatedByCoachId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

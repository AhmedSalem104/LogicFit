using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class DietPlanConfiguration : IEntityTypeConfiguration<DietPlan>
{
    public void Configure(EntityTypeBuilder<DietPlan> builder)
    {
        builder.ToTable("DietPlans");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(e => e.CalorieGoal).HasMaxLength(30);
        builder.Property(e => e.CalculatorMetadata).HasMaxLength(2000);
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.Version).HasDefaultValue(1);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.CoachId);
        builder.HasIndex(e => e.ClientId);

        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.DietPlans)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Coach)
            .WithMany(u => u.CoachDietPlans)
            .HasForeignKey(e => e.CoachId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Client)
            .WithMany(u => u.ClientDietPlans)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

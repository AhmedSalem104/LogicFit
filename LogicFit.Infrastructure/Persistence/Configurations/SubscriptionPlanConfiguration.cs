using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Price)
            .HasPrecision(18, 2);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.Features)
            .HasMaxLength(2000);

        builder.Property(e => e.MaxFreezeDays)
            .HasDefaultValue(0);

        builder.Property(e => e.MaxFreezeCount)
            .HasDefaultValue(0);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.InBodyIncluded)
            .HasDefaultValue(false);

        builder.Property(e => e.PrivateCoach)
            .HasDefaultValue(false);

        builder.HasIndex(e => e.TenantId);

        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.SubscriptionPlans)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

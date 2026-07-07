using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
{
    public void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        builder.ToTable("TenantSubscriptions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Currency).HasMaxLength(10).IsRequired();
        builder.Property(e => e.ApprovedBy).HasMaxLength(100);
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.Status);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Plan)
            .WithMany(p => p.TenantSubscriptions)
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

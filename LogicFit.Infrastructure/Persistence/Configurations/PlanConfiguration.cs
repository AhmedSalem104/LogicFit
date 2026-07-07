using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.Price).HasPrecision(18, 2);
        builder.Property(e => e.Currency).HasMaxLength(10).IsRequired();

        builder.HasIndex(e => e.Name).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasMany(e => e.PlanFeatures)
            .WithOne(pf => pf.Plan)
            .HasForeignKey(pf => pf.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.TenantSubscriptions)
            .WithOne(ts => ts.Plan)
            .HasForeignKey(ts => ts.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class FeatureConfiguration : IEntityTypeConfiguration<Feature>
{
    public void Configure(EntityTypeBuilder<Feature> builder)
    {
        builder.ToTable("Features");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => e.Code).IsUnique();

        builder.HasMany(e => e.PlanFeatures)
            .WithOne(pf => pf.Feature)
            .HasForeignKey(pf => pf.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

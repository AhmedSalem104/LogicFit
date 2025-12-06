using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class NutrientDefinitionConfiguration : IEntityTypeConfiguration<NutrientDefinition>
{
    public void Configure(EntityTypeBuilder<NutrientDefinition> builder)
    {
        builder.ToTable("NutrientDefinitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Unit)
            .HasMaxLength(20);

        builder.HasIndex(e => e.Name)
            .IsUnique();
    }
}

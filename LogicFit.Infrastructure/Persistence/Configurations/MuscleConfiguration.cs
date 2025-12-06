using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class MuscleConfiguration : IEntityTypeConfiguration<Muscle>
{
    public void Configure(EntityTypeBuilder<Muscle> builder)
    {
        builder.ToTable("Muscles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.BodyPart)
            .HasMaxLength(100);

        builder.HasIndex(e => e.BodyPart);
    }
}

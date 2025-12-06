using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class BodyMeasurementConfiguration : IEntityTypeConfiguration<BodyMeasurement>
{
    public void Configure(EntityTypeBuilder<BodyMeasurement> builder)
    {
        builder.ToTable("BodyMeasurements");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.WeightKg)
            .HasPrecision(10, 2);

        builder.Property(e => e.InbodyImageUrl)
            .HasMaxLength(500);

        builder.Property(e => e.FrontPhotoUrl)
            .HasMaxLength(500);

        builder.Property(e => e.SidePhotoUrl)
            .HasMaxLength(500);

        builder.Property(e => e.BackPhotoUrl)
            .HasMaxLength(500);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.ClientId);
        builder.HasIndex(e => e.DateRecorded);

        builder.HasOne(e => e.Client)
            .WithMany(u => u.BodyMeasurements)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

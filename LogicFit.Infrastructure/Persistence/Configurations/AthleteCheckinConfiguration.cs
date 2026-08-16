using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class AthleteCheckinConfiguration : IEntityTypeConfiguration<AthleteCheckin>
{
    public void Configure(EntityTypeBuilder<AthleteCheckin> builder)
    {
        builder.ToTable("AthleteCheckins");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SleepHours).HasPrecision(4, 1);
        builder.Property(x => x.Hrv).HasPrecision(8, 2);
        builder.Property(x => x.BodyweightKg).HasPrecision(10, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasIndex(x => new { x.TenantId, x.ClientId, x.CheckinDate }).IsUnique();
        builder.HasOne(x => x.Client)
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

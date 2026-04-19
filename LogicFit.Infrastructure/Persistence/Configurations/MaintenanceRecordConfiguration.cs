using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class MaintenanceRecordConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.ToTable("MaintenanceRecords");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.TechnicianName).HasMaxLength(200);
        builder.Property(e => e.TechnicianContact).HasMaxLength(100);
        builder.Property(e => e.ResolutionNotes).HasMaxLength(1000);
        builder.Property(e => e.Cost).HasPrecision(18, 2);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.EquipmentId);
        builder.HasIndex(e => e.Status);

        builder.HasOne(e => e.Equipment)
            .WithMany(eq => eq.MaintenanceRecords)
            .HasForeignKey(e => e.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

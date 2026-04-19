using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("Equipment");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.SerialNumber).HasMaxLength(100);
        builder.Property(e => e.Brand).HasMaxLength(100);
        builder.Property(e => e.Model).HasMaxLength(100);
        builder.Property(e => e.Category).HasMaxLength(100);
        builder.Property(e => e.ImageUrl).HasMaxLength(500);
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.PurchasePrice).HasPrecision(18, 2);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.BranchId);
        builder.HasIndex(e => e.RoomId);
        builder.HasIndex(e => new { e.TenantId, e.SerialNumber }).IsUnique().HasFilter("[SerialNumber] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Room)
            .WithMany(r => r.Equipment)
            .HasForeignKey(e => e.RoomId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

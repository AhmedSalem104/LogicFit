using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId)
            .HasMaxLength(100);

        builder.Property(e => e.EntityName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.EntityId)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.OldValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.NewValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.AffectedColumns)
            .HasMaxLength(2000);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(50);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.EntityName);
        builder.HasIndex(e => e.Timestamp);
    }
}

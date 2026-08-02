using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class DatabaseBackupConfiguration : IEntityTypeConfiguration<DatabaseBackup>
{
    public void Configure(EntityTypeBuilder<DatabaseBackup> builder)
    {
        builder.ToTable("DatabaseBackups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DatabaseName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(512);
        builder.Property(x => x.Sha256).HasMaxLength(64);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.BackupBatchId, x.TenantId });
    }
}

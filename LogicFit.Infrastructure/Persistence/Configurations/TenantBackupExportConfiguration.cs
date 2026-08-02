using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class TenantBackupExportConfiguration : IEntityTypeConfiguration<TenantBackupExport>
{
    public void Configure(EntityTypeBuilder<TenantBackupExport> builder)
    {
        builder.ToTable("TenantBackupExports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status, x.CreatedAt });
        builder.HasOne(x => x.BackupBatch).WithMany().HasForeignKey(x => x.BackupBatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DatabaseBackup).WithMany().HasForeignKey(x => x.DatabaseBackupId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.DownloadGrants).WithOne(x => x.TenantBackupExport)
            .HasForeignKey(x => x.TenantBackupExportId).OnDelete(DeleteBehavior.Cascade);
    }
}

using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class BackupBatchConfiguration : IEntityTypeConfiguration<BackupBatch>
{
    public void Configure(EntityTypeBuilder<BackupBatch> builder)
    {
        builder.ToTable("BackupBatches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ManifestStorageKey).HasMaxLength(512);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.HasIndex(x => new { x.Status, x.StartedAtUtc });
        builder.HasMany(x => x.Artifacts)
            .WithOne(x => x.BackupBatch)
            .HasForeignKey(x => x.BackupBatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

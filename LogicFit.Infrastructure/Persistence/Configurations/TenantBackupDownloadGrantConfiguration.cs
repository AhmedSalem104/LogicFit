using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class TenantBackupDownloadGrantConfiguration : IEntityTypeConfiguration<TenantBackupDownloadGrant>
{
    public void Configure(EntityTypeBuilder<TenantBackupDownloadGrant> builder)
    {
        builder.ToTable("TenantBackupDownloadGrants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedByIp).HasMaxLength(64);
        builder.Property(x => x.ConsumedByIp).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.TenantBackupExportId, x.UserId, x.ExpiresAtUtc });
    }
}

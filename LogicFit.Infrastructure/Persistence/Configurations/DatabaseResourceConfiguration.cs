using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class DatabaseResourceConfiguration : IEntityTypeConfiguration<DatabaseResource>
{
    public void Configure(EntityTypeBuilder<DatabaseResource> builder)
    {
        builder.ToTable("DatabaseResources");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Provider).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DatabaseName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ServerKey).HasMaxLength(256);
        builder.Property(x => x.EncryptedConnectionString).HasMaxLength(4096);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.SchemaVersion).HasMaxLength(64);
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.Provider, x.DatabaseName }).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ReservedForTenantId);
    }
}

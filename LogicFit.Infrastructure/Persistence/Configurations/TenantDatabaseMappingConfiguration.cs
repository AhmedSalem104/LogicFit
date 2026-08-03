using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class TenantDatabaseMappingConfiguration : IEntityTypeConfiguration<TenantDatabaseMapping>
{
    public void Configure(EntityTypeBuilder<TenantDatabaseMapping> builder)
    {
        builder.ToTable("TenantDatabaseMappings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Provider).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EncryptedConnectionString).HasMaxLength(4096).IsRequired();
        builder.Property(x => x.SchemaVersion).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.TenantId, x.IsActive })
            .IsUnique()
            .HasFilter("[IsActive] = 1");
        builder.HasIndex(x => new { x.DatabaseResourceId, x.IsActive })
            .IsUnique()
            .HasFilter("[IsActive] = 1");
        // The resource is a row in the same Platform DB today.  The Tenant DB never receives this FK.
        builder.HasOne<DatabaseResource>()
            .WithMany()
            .HasForeignKey(x => x.DatabaseResourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

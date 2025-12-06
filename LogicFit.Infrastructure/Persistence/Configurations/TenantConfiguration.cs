using LogicFit.Domain.Entities;
using LogicFit.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Subdomain)
            .HasMaxLength(100);

        builder.HasIndex(e => e.Subdomain)
            .IsUnique()
            .HasFilter("[Subdomain] IS NOT NULL");

        builder.Property(e => e.BrandingSettings)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<BrandingSettings>(v, (JsonSerializerOptions?)null))
            .HasColumnType("nvarchar(max)");
    }
}

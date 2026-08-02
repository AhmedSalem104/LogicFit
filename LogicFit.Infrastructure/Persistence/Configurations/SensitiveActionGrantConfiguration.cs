using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class SensitiveActionGrantConfiguration : IEntityTypeConfiguration<SensitiveActionGrant>
{
    public void Configure(EntityTypeBuilder<SensitiveActionGrant> builder)
    {
        builder.ToTable("SensitiveActionGrants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Scope).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedByIp).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.TenantId, x.Scope, x.ExpiresAtUtc });
    }
}

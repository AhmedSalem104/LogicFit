using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class TenantUsageConfiguration : IEntityTypeConfiguration<TenantUsage>
{
    public void Configure(EntityTypeBuilder<TenantUsage> builder)
    {
        builder.ToTable("TenantUsages");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.TenantId).IsUnique();
    }
}

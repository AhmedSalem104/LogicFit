using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceClientJoinCodeConfiguration : IEntityTypeConfiguration<WorkspaceClientJoinCode>
{
    public void Configure(EntityTypeBuilder<WorkspaceClientJoinCode> builder)
    {
        builder.ToTable("WorkspaceClientJoinCodes");
        builder.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.CodeHash).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.RevokedAt })
            .HasDatabaseName("IX_WorkspaceClientJoinCodes_OneActivePerWorkspace")
            .HasFilter("[RevokedAt] IS NULL").IsUnique();
        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class IdentityWorkspaceSessionConfiguration : IEntityTypeConfiguration<IdentityWorkspaceSession>
{
    public void Configure(EntityTypeBuilder<IdentityWorkspaceSession> builder)
    {
        builder.ToTable("IdentityWorkspaceSessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedByIp).HasMaxLength(64);
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.IdentityAccountId, x.ExpiresAt });
        builder.HasOne(x => x.IdentityAccount).WithMany().HasForeignKey(x => x.IdentityAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

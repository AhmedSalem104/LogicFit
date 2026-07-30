using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class IdentityPasskeyStepUpSessionConfiguration : IEntityTypeConfiguration<IdentityPasskeyStepUpSession>
{
    public void Configure(EntityTypeBuilder<IdentityPasskeyStepUpSession> builder)
    {
        builder.ToTable("IdentityPasskeyStepUpSessions");
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.IdentityAccountId, x.ExpiresAt });
        builder.HasOne(x => x.IdentityAccount).WithMany().HasForeignKey(x => x.IdentityAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

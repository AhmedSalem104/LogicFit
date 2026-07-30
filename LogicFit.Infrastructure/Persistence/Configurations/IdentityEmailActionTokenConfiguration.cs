using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class IdentityEmailActionTokenConfiguration : IEntityTypeConfiguration<IdentityEmailActionToken>
{
    public void Configure(EntityTypeBuilder<IdentityEmailActionToken> builder)
    {
        builder.ToTable("IdentityEmailActionTokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedByIp).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.IdentityAccountId, x.Purpose, x.ExpiresAt });
        builder.HasOne(x => x.IdentityAccount)
            .WithMany(x => x.EmailActionTokens)
            .HasForeignKey(x => x.IdentityAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

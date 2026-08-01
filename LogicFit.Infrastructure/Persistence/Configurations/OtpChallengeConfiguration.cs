using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class OtpChallengeConfiguration : IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(EntityTypeBuilder<OtpChallenge> builder)
    {
        builder.ToTable("OtpChallenges");
        builder.Property(x => x.NormalizedPhoneNumber).HasMaxLength(16).IsRequired();
        builder.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CodeSalt).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ProviderMessageId).HasMaxLength(256);
        builder.Property(x => x.SessionBinding).HasMaxLength(128);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.NormalizedPhoneNumber, x.Purpose, x.Status });
        builder.HasIndex(x => x.ProviderMessageId);
        builder.HasOne(x => x.IdentityAccount).WithMany(x => x.OtpChallenges)
            .HasForeignKey(x => x.IdentityAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

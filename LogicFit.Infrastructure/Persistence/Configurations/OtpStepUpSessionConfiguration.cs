using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class OtpStepUpSessionConfiguration : IEntityTypeConfiguration<OtpStepUpSession>
{
    public void Configure(EntityTypeBuilder<OtpStepUpSession> builder)
    {
        builder.ToTable("OtpStepUpSessions");
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SessionBinding).HasMaxLength(128);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.IdentityAccountId, x.ExpiresAtUtc });
        builder.HasOne<IdentityAccount>().WithMany()
            .HasForeignKey(x => x.IdentityAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OtpChallenge>().WithMany()
            .HasForeignKey(x => x.OtpChallengeId).OnDelete(DeleteBehavior.Restrict);
    }
}

using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Token).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Surface).HasMaxLength(20).IsRequired();
        builder.Property(e => e.CreatedByIp).HasMaxLength(64);
        builder.Property(e => e.RevokedByIp).HasMaxLength(64);
        builder.Property(e => e.ReplacedByToken).HasMaxLength(256);

        builder.Ignore(e => e.IsActive);

        builder.HasIndex(e => e.Token).IsUnique();
        builder.HasIndex(e => e.UserId);

        builder.HasOne(e => e.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

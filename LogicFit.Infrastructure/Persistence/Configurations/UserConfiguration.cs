using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("DomainUsers");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.WalletBalance)
            .HasPrecision(18, 2);

        builder.HasIndex(e => new { e.TenantId, e.Email })
            .IsUnique();

        builder.HasIndex(e => e.TenantId);

        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class IdentityPasskeyCredentialConfiguration : IEntityTypeConfiguration<IdentityPasskeyCredential>
{
    public void Configure(EntityTypeBuilder<IdentityPasskeyCredential> builder)
    {
        builder.ToTable("IdentityPasskeyCredentials");
        builder.Property(x => x.CredentialId).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.PublicKey).HasMaxLength(4096).IsRequired();
        builder.Property(x => x.UserHandle).HasMaxLength(128).IsRequired();
        builder.Property(x => x.FriendlyName).HasMaxLength(120);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.CredentialId).IsUnique();
        builder.HasIndex(x => new { x.IdentityAccountId, x.IsActive });
        builder.HasOne(x => x.IdentityAccount).WithMany(x => x.PasskeyCredentials)
            .HasForeignKey(x => x.IdentityAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

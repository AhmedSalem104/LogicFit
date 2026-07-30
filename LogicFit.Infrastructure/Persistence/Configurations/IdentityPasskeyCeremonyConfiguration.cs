using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class IdentityPasskeyCeremonyConfiguration : IEntityTypeConfiguration<IdentityPasskeyCeremony>
{
    public void Configure(EntityTypeBuilder<IdentityPasskeyCeremony> builder)
    {
        builder.ToTable("IdentityPasskeyCeremonies");
        builder.Property(x => x.OptionsJson).HasMaxLength(16000).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.IdentityAccountId, x.Purpose, x.ExpiresAt });
        builder.HasOne(x => x.IdentityAccount).WithMany().HasForeignKey(x => x.IdentityAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class MembershipCardConfiguration : IEntityTypeConfiguration<MembershipCard>
{
    public void Configure(EntityTypeBuilder<MembershipCard> builder)
    {
        builder.ToTable("MembershipCards");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CardNumber).HasMaxLength(100).IsRequired();
        builder.Property(e => e.QrCode).HasMaxLength(200).IsRequired();
        builder.Property(e => e.RevokedReason).HasMaxLength(500);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.CardNumber }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(e => new { e.TenantId, e.QrCode }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(e => e.ClientId);

        builder.HasOne(e => e.Client)
            .WithMany()
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

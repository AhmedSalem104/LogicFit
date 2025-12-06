using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class SubscriptionFreezeConfiguration : IEntityTypeConfiguration<SubscriptionFreeze>
{
    public void Configure(EntityTypeBuilder<SubscriptionFreeze> builder)
    {
        builder.ToTable("SubscriptionFreezes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Reason)
            .HasMaxLength(500);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.SubscriptionId);

        builder.HasOne(e => e.Subscription)
            .WithMany(s => s.Freezes)
            .HasForeignKey(e => e.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class CouponUsageConfiguration : IEntityTypeConfiguration<CouponUsage>
{
    public void Configure(EntityTypeBuilder<CouponUsage> builder)
    {
        builder.ToTable("CouponUsages");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.DiscountApplied).HasPrecision(18, 2);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.CouponId);
        builder.HasIndex(e => e.UserId);

        builder.HasOne(e => e.Coupon).WithMany(c => c.Usages).HasForeignKey(e => e.CouponId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Invoice).WithMany().HasForeignKey(e => e.InvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

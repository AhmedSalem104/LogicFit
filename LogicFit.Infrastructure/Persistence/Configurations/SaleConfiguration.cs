using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SaleNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Subtotal).HasPrecision(18, 2);
        builder.Property(e => e.TaxAmount).HasPrecision(18, 2);
        builder.Property(e => e.DiscountAmount).HasPrecision(18, 2);
        builder.Property(e => e.Total).HasPrecision(18, 2);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.BranchId);
        builder.HasIndex(e => e.ClientId);
        builder.HasIndex(e => e.CashierId);
        builder.HasIndex(e => e.SaleDate);
        builder.HasIndex(e => new { e.TenantId, e.SaleNumber }).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Cashier).WithMany().HasForeignKey(e => e.CashierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Invoice).WithMany().HasForeignKey(e => e.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Coupon).WithMany().HasForeignKey(e => e.CouponId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Quantity).HasPrecision(18, 2);
        builder.Property(e => e.UnitPrice).HasPrecision(18, 2);
        builder.Property(e => e.TaxRate).HasPrecision(5, 2);
        builder.Property(e => e.DiscountAmount).HasPrecision(18, 2);
        builder.Property(e => e.LineTotal).HasPrecision(18, 2);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.SaleId);

        builder.HasOne(e => e.Sale).WithMany(s => s.Items).HasForeignKey(e => e.SaleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

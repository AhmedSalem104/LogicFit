using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.Sku).HasMaxLength(100);
        builder.Property(e => e.Barcode).HasMaxLength(100);
        builder.Property(e => e.Unit).HasMaxLength(20);
        builder.Property(e => e.ImageUrl).HasMaxLength(500);
        builder.Property(e => e.CostPrice).HasPrecision(18, 2);
        builder.Property(e => e.SellingPrice).HasPrecision(18, 2);
        builder.Property(e => e.TaxRate).HasPrecision(5, 2);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.CategoryId);
        builder.HasIndex(e => new { e.TenantId, e.Sku }).IsUnique().HasFilter("[Sku] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(e => new { e.TenantId, e.Barcode }).IsUnique().HasFilter("[Barcode] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasOne(e => e.Category).WithMany(c => c.Products).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

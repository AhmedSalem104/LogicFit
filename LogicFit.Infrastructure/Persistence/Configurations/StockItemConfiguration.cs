using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Quantity).HasPrecision(18, 2);

        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.ProductId, e.BranchId }).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasOne(e => e.Product).WithMany(p => p.StockItems).HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

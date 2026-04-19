using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Quantity).HasPrecision(18, 2);
        builder.Property(e => e.QuantityAfter).HasPrecision(18, 2);
        builder.Property(e => e.Reason).HasMaxLength(500);
        builder.Property(e => e.ReferenceType).HasMaxLength(50);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.BranchId);
        builder.HasIndex(e => e.MovedAt);

        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.MovedBy).WithMany().HasForeignKey(e => e.MovedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.TargetBranch).WithMany().HasForeignKey(e => e.TargetBranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

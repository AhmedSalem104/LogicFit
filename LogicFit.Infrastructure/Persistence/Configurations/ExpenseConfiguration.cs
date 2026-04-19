using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.VendorName).HasMaxLength(200);
        builder.Property(e => e.ReceiptImageUrl).HasMaxLength(500);
        builder.Property(e => e.ReferenceNumber).HasMaxLength(100);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.BranchId);
        builder.HasIndex(e => e.CategoryId);
        builder.HasIndex(e => e.ExpenseDate);

        builder.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Category).WithMany(c => c.Expenses).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.ApprovedBy).WithMany().HasForeignKey(e => e.ApprovedById).OnDelete(DeleteBehavior.Restrict);
    }
}

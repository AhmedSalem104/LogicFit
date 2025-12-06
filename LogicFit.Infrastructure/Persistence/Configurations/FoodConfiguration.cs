using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class FoodConfiguration : IEntityTypeConfiguration<Food>
{
    public void Configure(EntityTypeBuilder<Food> builder)
    {
        builder.ToTable("Foods");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Category)
            .HasMaxLength(100);

        builder.Property(e => e.AlternativeGroupId)
            .HasMaxLength(50);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.AlternativeGroupId);

        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.Foods)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

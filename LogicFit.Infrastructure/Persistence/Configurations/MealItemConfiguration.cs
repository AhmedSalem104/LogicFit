using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class MealItemConfiguration : IEntityTypeConfiguration<MealItem>
{
    public void Configure(EntityTypeBuilder<MealItem> builder)
    {
        builder.ToTable("MealItems");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.MealId);

        builder.HasOne(e => e.Meal)
            .WithMany(m => m.Items)
            .HasForeignKey(e => e.MealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Food)
            .WithMany(f => f.MealItems)
            .HasForeignKey(e => e.FoodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

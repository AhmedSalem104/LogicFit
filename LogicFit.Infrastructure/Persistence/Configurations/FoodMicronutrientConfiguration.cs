using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class FoodMicronutrientConfiguration : IEntityTypeConfiguration<FoodMicronutrient>
{
    public void Configure(EntityTypeBuilder<FoodMicronutrient> builder)
    {
        builder.ToTable("FoodMicronutrients");

        builder.HasKey(e => new { e.FoodId, e.NutrientId });

        builder.HasOne(e => e.Food)
            .WithMany(f => f.Micronutrients)
            .HasForeignKey(e => e.FoodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Nutrient)
            .WithMany(n => n.FoodMicronutrients)
            .HasForeignKey(e => e.NutrientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

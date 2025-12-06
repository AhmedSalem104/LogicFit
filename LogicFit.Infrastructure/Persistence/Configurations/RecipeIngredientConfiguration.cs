using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.ToTable("RecipeIngredients");

        builder.HasKey(e => new { e.RecipeId, e.FoodId });

        builder.HasOne(e => e.Recipe)
            .WithMany(r => r.Ingredients)
            .HasForeignKey(e => e.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Food)
            .WithMany(f => f.RecipeIngredients)
            .HasForeignKey(e => e.FoodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

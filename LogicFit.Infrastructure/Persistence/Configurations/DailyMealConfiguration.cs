using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class DailyMealConfiguration : IEntityTypeConfiguration<DailyMeal>
{
    public void Configure(EntityTypeBuilder<DailyMeal> builder)
    {
        builder.ToTable("DailyMeals");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.PlanId);

        builder.HasOne(e => e.Plan)
            .WithMany(p => p.Meals)
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

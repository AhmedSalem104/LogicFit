using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class MealLogConfiguration : IEntityTypeConfiguration<MealLog>
{
    public void Configure(EntityTypeBuilder<MealLog> builder)
    {
        builder.ToTable("MealLogs");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.MealNameSnapshot).HasMaxLength(200);
        builder.Property(e => e.FoodNameSnapshot).HasMaxLength(200);
        builder.Property(e => e.FoodUnitSnapshot).HasMaxLength(40);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.ClientId);
        builder.HasIndex(e => e.ConsumedAt);

        builder.HasOne(e => e.Client)
            .WithMany(u => u.MealLogs)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.MealItem)
            .WithMany(m => m.Logs)
            .HasForeignKey(e => e.MealItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.AlternativeFood)
            .WithMany(f => f.AlternativeMealLogs)
            .HasForeignKey(e => e.AlternativeFoodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

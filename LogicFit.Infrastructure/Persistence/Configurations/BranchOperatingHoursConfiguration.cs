using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class BranchOperatingHoursConfiguration : IEntityTypeConfiguration<BranchOperatingHours>
{
    public void Configure(EntityTypeBuilder<BranchOperatingHours> builder)
    {
        builder.ToTable("BranchOperatingHours");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.BranchId, e.DayOfWeek }).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasOne(e => e.Branch)
            .WithMany(b => b.OperatingHours)
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

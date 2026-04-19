using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class ClassScheduleConfiguration : IEntityTypeConfiguration<ClassSchedule>
{
    public void Configure(EntityTypeBuilder<ClassSchedule> builder)
    {
        builder.ToTable("ClassSchedules");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RecurrenceDaysOfWeek).HasMaxLength(50);
        builder.Property(e => e.CancellationReason).HasMaxLength(500);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.GroupClassId);
        builder.HasIndex(e => e.CoachId);
        builder.HasIndex(e => e.RoomId);
        builder.HasIndex(e => e.StartTime);

        builder.HasOne(e => e.GroupClass).WithMany(c => c.Schedules).HasForeignKey(e => e.GroupClassId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Coach).WithMany().HasForeignKey(e => e.CoachId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Room).WithMany().HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Restrict);
    }
}

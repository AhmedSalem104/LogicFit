using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class ClassEnrollmentConfiguration : IEntityTypeConfiguration<ClassEnrollment>
{
    public void Configure(EntityTypeBuilder<ClassEnrollment> builder)
    {
        builder.ToTable("ClassEnrollments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CancellationReason).HasMaxLength(500);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.ScheduleId);
        builder.HasIndex(e => e.ClientId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.ScheduleId, e.ClientId }).IsUnique().HasFilter("[IsDeleted] = 0 AND [Status] <> 3");

        builder.HasOne(e => e.Schedule).WithMany(s => s.Enrollments).HasForeignKey(e => e.ScheduleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Restrict);
    }
}

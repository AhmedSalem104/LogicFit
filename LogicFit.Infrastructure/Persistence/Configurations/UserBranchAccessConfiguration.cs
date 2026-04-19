using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class UserBranchAccessConfiguration : IEntityTypeConfiguration<UserBranchAccess>
{
    public void Configure(EntityTypeBuilder<UserBranchAccess> builder)
    {
        builder.ToTable("UserBranchAccesses");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.UserId, e.BranchId }).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasOne(e => e.User)
            .WithMany(u => u.BranchAccesses)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Branch)
            .WithMany(b => b.UserAccesses)
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

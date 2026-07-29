using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class WorkspaceMembershipConfiguration : IEntityTypeConfiguration<WorkspaceMembership>
{
    public void Configure(EntityTypeBuilder<WorkspaceMembership> builder)
    {
        builder.ToTable("WorkspaceMemberships");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DecisionReason).HasMaxLength(1000);
        builder.Property(x => x.ApprovedBy).HasMaxLength(100);
        builder.Property(x => x.RejectedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.IdentityAccountId, x.TenantId }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne(x => x.IdentityAccount).WithMany(x => x.Memberships).HasForeignKey(x => x.IdentityAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Tenant).WithMany(x => x.WorkspaceMemberships).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SponsoredByMembership).WithMany().HasForeignKey(x => x.SponsoredByMembershipId).OnDelete(DeleteBehavior.Restrict);
    }
}

using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceInviteConfiguration : IEntityTypeConfiguration<WorkspaceInvite>
{
    public void Configure(EntityTypeBuilder<WorkspaceInvite> builder)
    {
        builder.ToTable("WorkspaceInvites");
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.NormalizedEmail, x.Role, x.Status })
            .HasDatabaseName("IX_WorkspaceInvites_ActiveEmailRole")
            .IsUnique()
            .HasFilter("[Status] = 1");
        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.InvitedByMembership).WithMany().HasForeignKey(x => x.InvitedByMembershipId).OnDelete(DeleteBehavior.Restrict);
    }
}

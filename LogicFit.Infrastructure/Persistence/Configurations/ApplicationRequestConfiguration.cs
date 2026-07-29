using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class ApplicationRequestConfiguration : IEntityTypeConfiguration<ApplicationRequest>
{
    public void Configure(EntityTypeBuilder<ApplicationRequest> builder)
    {
        builder.ToTable("ApplicationRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.InformationRequest).HasMaxLength(2000);
        builder.Property(x => x.RequestedFieldsJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.DecisionReason).HasMaxLength(2000);
        builder.Property(x => x.TargetScopeKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ReservedWorkspaceIdentifier).HasMaxLength(100);
        builder.Property(x => x.ReviewedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.IdentityAccountId, x.TargetWorkspaceId, x.ApplicationType, x.Status });
        builder.HasIndex(x => new { x.IdentityAccountId, x.TargetScopeKey, x.ApplicationType })
            .IsUnique()
            .HasFilter("[Status] IN (1, 2, 3, 4)");
        builder.HasIndex(x => x.ReservedWorkspaceIdentifier).IsUnique().HasFilter("[ReservedWorkspaceIdentifier] IS NOT NULL AND [Status] IN (1, 2, 3, 4)");
        builder.HasOne(x => x.IdentityAccount).WithMany(x => x.Applications).HasForeignKey(x => x.IdentityAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TargetWorkspace).WithMany().HasForeignKey(x => x.TargetWorkspaceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.ProvisionedWorkspaceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkspaceMembership>().WithMany().HasForeignKey(x => x.SponsoredByMembershipId).OnDelete(DeleteBehavior.Restrict);
    }
}

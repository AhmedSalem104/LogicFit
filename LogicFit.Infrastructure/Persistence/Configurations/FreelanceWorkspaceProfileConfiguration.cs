using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class FreelanceWorkspaceProfileConfiguration : IEntityTypeConfiguration<FreelanceWorkspaceProfile>
{
    public void Configure(EntityTypeBuilder<FreelanceWorkspaceProfile> builder)
    {
        builder.ToTable("FreelanceWorkspaceProfiles");
        builder.HasKey(x => x.TenantId);
        builder.Property(x => x.Bio).HasMaxLength(4000);
        builder.Property(x => x.WelcomeMessage).HasMaxLength(1000);
        builder.HasOne(x => x.Tenant).WithOne().HasForeignKey<FreelanceWorkspaceProfile>(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

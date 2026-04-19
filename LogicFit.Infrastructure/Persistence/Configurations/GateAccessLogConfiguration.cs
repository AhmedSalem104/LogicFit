using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class GateAccessLogConfiguration : IEntityTypeConfiguration<GateAccessLog>
{
    public void Configure(EntityTypeBuilder<GateAccessLog> builder)
    {
        builder.ToTable("GateAccessLogs");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.ScannedCode).HasMaxLength(200);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.ClientId);
        builder.HasIndex(e => e.BranchId);
        builder.HasIndex(e => e.AccessTime);

        builder.HasOne(e => e.Client)
            .WithMany()
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.MembershipCard)
            .WithMany()
            .HasForeignKey(e => e.MembershipCardId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

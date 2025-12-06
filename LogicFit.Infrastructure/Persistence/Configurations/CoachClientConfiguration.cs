using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class CoachClientConfiguration : IEntityTypeConfiguration<CoachClient>
{
    public void Configure(EntityTypeBuilder<CoachClient> builder)
    {
        builder.ToTable("CoachClients");

        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.CoachId);
        builder.HasIndex(e => new { e.TenantId, e.ClientId, e.IsActive })
            .HasFilter("[IsActive] = 1")
            .IsUnique(); // Unique active coach per client per tenant

        // Relationships
        builder.HasOne(e => e.Coach)
            .WithMany(u => u.Trainees)
            .HasForeignKey(e => e.CoachId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Client)
            .WithMany(u => u.AssignedCoaches)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Properties
        builder.Property(e => e.Notes)
            .HasMaxLength(500);
    }
}

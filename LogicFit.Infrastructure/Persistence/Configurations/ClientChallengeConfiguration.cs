using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class ClientChallengeConfiguration : IEntityTypeConfiguration<ClientChallenge>
{
    public void Configure(EntityTypeBuilder<ClientChallenge> builder)
    {
        builder.ToTable("ClientChallenges");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.ChallengeId, e.ClientId }).IsUnique();

        builder.HasOne(e => e.Challenge)
            .WithMany(c => c.Participants)
            .HasForeignKey(e => e.ChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Client)
            .WithMany()
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

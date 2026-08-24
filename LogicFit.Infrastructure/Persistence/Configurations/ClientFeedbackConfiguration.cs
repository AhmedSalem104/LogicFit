using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class ClientFeedbackConfiguration : IEntityTypeConfiguration<ClientFeedback>
{
    public void Configure(EntityTypeBuilder<ClientFeedback> builder)
    {
        builder.ToTable("ClientFeedback");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NoteType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(24).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.SubmittedAt });
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.ClientId });
        builder.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedById).OnDelete(DeleteBehavior.Restrict);
    }
}

using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class PaymentProofConfiguration : IEntityTypeConfiguration<PaymentProof>
{
    public void Configure(EntityTypeBuilder<PaymentProof> builder)
    {
        builder.ToTable("PaymentProofs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
        builder.Property(x => x.UploadedBy).HasMaxLength(100);
        builder.HasIndex(x => new { x.PaymentRequestId, x.Version }).IsUnique();
        builder.HasIndex(x => new { x.PaymentRequestId, x.IsCurrent });
        builder.HasOne(x => x.PaymentRequest)
            .WithMany(x => x.Proofs)
            .HasForeignKey(x => x.PaymentRequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

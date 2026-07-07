using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class PaymentRequestConfiguration : IEntityTypeConfiguration<PaymentRequest>
{
    public void Configure(EntityTypeBuilder<PaymentRequest> builder)
    {
        builder.ToTable("PaymentRequests");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Currency).HasMaxLength(10).IsRequired();
        builder.Property(e => e.TransactionNumber).HasMaxLength(100);
        builder.Property(e => e.ProofFileUrl).HasMaxLength(500);
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.ReviewedBy).HasMaxLength(100);
        builder.Property(e => e.RejectReason).HasMaxLength(500);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.Status);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Plan)
            .WithMany()
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TenantSubscription)
            .WithMany()
            .HasForeignKey(e => e.TenantSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PaymentMethod)
            .WithMany()
            .HasForeignKey(e => e.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

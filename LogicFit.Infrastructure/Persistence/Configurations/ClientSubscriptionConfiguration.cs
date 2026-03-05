using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class ClientSubscriptionConfiguration : IEntityTypeConfiguration<ClientSubscription>
{
    public void Configure(EntityTypeBuilder<ClientSubscription> builder)
    {
        builder.ToTable("ClientSubscriptions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.AmountPaid)
            .HasPrecision(18, 2);

        builder.Property(e => e.Discount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.ClientId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.RenewedFromId);

        builder.HasOne(e => e.Client)
            .WithMany(u => u.Subscriptions)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Plan)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SalesCoach)
            .WithMany(u => u.SalesSubscriptions)
            .HasForeignKey(e => e.SalesCoachId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RenewedFrom)
            .WithMany()
            .HasForeignKey(e => e.RenewedFromId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

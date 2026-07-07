using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class TenantPaymentMethodConfiguration : IEntityTypeConfiguration<TenantPaymentMethod>
{
    public void Configure(EntityTypeBuilder<TenantPaymentMethod> builder)
    {
        builder.ToTable("TenantPaymentMethods");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(50);
        builder.Property(e => e.AccountName).HasMaxLength(200);
        builder.Property(e => e.AccountNumber).HasMaxLength(100);
        builder.Property(e => e.IBAN).HasMaxLength(64);
        builder.Property(e => e.WalletNumber).HasMaxLength(50);
        builder.Property(e => e.Instructions).HasMaxLength(2000);
        builder.Property(e => e.QRImageUrl).HasMaxLength(500);
    }
}

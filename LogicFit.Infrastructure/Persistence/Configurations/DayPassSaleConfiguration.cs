using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class DayPassSaleConfiguration : IEntityTypeConfiguration<DayPassSale>
{
    public void Configure(EntityTypeBuilder<DayPassSale> builder)
    {
        builder.ToTable("DayPassSales");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReferenceNumber).HasMaxLength(40).IsRequired();
        builder.Property(x => x.VisitorName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.VisitorPhone).HasMaxLength(30);
        builder.Property(x => x.VisitorPhoneNormalized).HasMaxLength(30);
        builder.Property(x => x.PassTypeCodeSnapshot).HasMaxLength(40).IsRequired();
        builder.Property(x => x.PassTypeNameSnapshot).HasMaxLength(120).IsRequired();
        builder.Property(x => x.AmountDue).HasPrecision(18, 2);
        builder.Property(x => x.AmountPaid).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.ReferenceNumber }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.VisitDate, x.Id });
        builder.HasIndex(x => new { x.TenantId, x.Status, x.VisitDate });
        builder.HasIndex(x => new { x.TenantId, x.VisitorPhoneNormalized, x.VisitDate });

        builder.HasOne(x => x.DayPassType)
            .WithMany(x => x.Sales)
            .HasForeignKey(x => x.DayPassTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

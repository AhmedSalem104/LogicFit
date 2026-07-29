using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class ApplicationRequestRevisionConfiguration : IEntityTypeConfiguration<ApplicationRequestRevision>
{
    public void Configure(EntityTypeBuilder<ApplicationRequestRevision> builder)
    {
        builder.ToTable("ApplicationRequestRevisions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.SubmittedBy).HasMaxLength(100);
        builder.HasIndex(x => new { x.ApplicationRequestId, x.RevisionNumber }).IsUnique();
        builder.HasOne(x => x.ApplicationRequest).WithMany(x => x.Revisions).HasForeignKey(x => x.ApplicationRequestId).OnDelete(DeleteBehavior.Restrict);
    }
}

using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class ProgramRoutineConfiguration : IEntityTypeConfiguration<ProgramRoutine>
{
    public void Configure(EntityTypeBuilder<ProgramRoutine> builder)
    {
        builder.ToTable("ProgramRoutines");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.ProgramId);

        builder.HasOne(e => e.Program)
            .WithMany(p => p.Routines)
            .HasForeignKey(e => e.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.Property(message => message.Type)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(message => message.IdempotencyKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(message => message.IdempotencyKey)
            .IsUnique();

        builder.HasIndex(message => new { message.ProcessedAtUtc, message.OccurredAtUtc });
    }
}

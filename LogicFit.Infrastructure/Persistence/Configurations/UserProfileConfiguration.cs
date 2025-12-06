using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(e => e.UserId);

        builder.Property(e => e.FullName)
            .HasMaxLength(200);

        builder.Property(e => e.ActivityLevel)
            .HasMaxLength(50);

        builder.Property(e => e.HeightCm)
            .HasPrecision(5, 2);
    }
}

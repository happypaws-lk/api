using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(u => u.Name).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(320);
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.AvatarKey).HasMaxLength(500);

        builder.Property(u => u.LastKnownLocation)
            .HasColumnType("geography (point, 4326)");
        builder.HasIndex(u => u.LastKnownLocation).HasMethod("gist");

        builder.Property(u => u.IsVerified).HasDefaultValue(false);
        builder.Property(u => u.ReputationPoints).HasDefaultValue(0);
        builder.Property(u => u.IsSuspended).HasDefaultValue(false);
        builder.Property(u => u.SuspendedReason).HasMaxLength(1000);
    }
}

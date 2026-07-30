using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class LifestyleProfileConfiguration : IEntityTypeConfiguration<LifestyleProfile>
{
    public void Configure(EntityTypeBuilder<LifestyleProfile> builder)
    {
        builder.ToTable("lifestyle_profiles");

        builder.HasKey(lp => lp.Id);
        builder.Property(lp => lp.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(lp => lp.HomeSize)
            .HasConversion<string>()
            .HasMaxLength(15);

        builder.Property(lp => lp.ActivityLevel)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(lp => lp.ExistingPetTypes)
            .HasColumnType("text[]");

        builder.HasIndex(lp => lp.ExistingPetTypes)
            .HasMethod("gin");

        builder.HasIndex(lp => lp.UserId).IsUnique();

        builder.HasOne(lp => lp.User)
            .WithOne(u => u.LifestyleProfile)
            .HasForeignKey<LifestyleProfile>(lp => lp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

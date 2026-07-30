using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class UserBadgeConfiguration : IEntityTypeConfiguration<UserBadge>
{
    public void Configure(EntityTypeBuilder<UserBadge> builder)
    {
        builder.ToTable("user_badges");

        builder.HasKey(ub => ub.Id);
        builder.Property(ub => ub.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(ub => ub.BadgeType)
            .HasConversion<string>()
            .HasMaxLength(25);

        builder.HasOne(ub => ub.User)
            .WithMany(u => u.Badges)
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

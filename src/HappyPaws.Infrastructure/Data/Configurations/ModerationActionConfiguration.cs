using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class ModerationActionConfiguration : IEntityTypeConfiguration<ModerationAction>
{
    public void Configure(EntityTypeBuilder<ModerationAction> builder)
    {
        builder.ToTable("moderation_actions");

        builder.HasKey(ma => ma.Id);
        builder.Property(ma => ma.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(ma => ma.TargetType)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(ma => ma.ActionType)
            .HasConversion<string>()
            .HasMaxLength(15);

        builder.Property(ma => ma.Reason).IsRequired().HasMaxLength(2000);

        builder.HasOne(ma => ma.Admin)
            .WithMany()
            .HasForeignKey(ma => ma.AdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

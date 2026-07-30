using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class RescueCaseConfiguration : IEntityTypeConfiguration<RescueCase>
{
    public void Configure(EntityTypeBuilder<RescueCase> builder)
    {
        builder.ToTable("rescue_cases");

        builder.HasKey(rc => rc.Id);
        builder.Property(rc => rc.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(rc => rc.LocationCoords)
            .HasColumnType("geography (point, 4326)");
        builder.HasIndex(rc => rc.LocationCoords).HasMethod("gist");

        builder.Property(rc => rc.LocationName).IsRequired().HasMaxLength(500);
        builder.Property(rc => rc.Description).IsRequired();
        builder.Property(rc => rc.PhotoKey).IsRequired().HasMaxLength(500);
        builder.Property(rc => rc.ConditionNotes).HasMaxLength(2000);

        builder.Property(rc => rc.OriginalAiUrgency)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(rc => rc.UrgencySource)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(rc => rc.Urgency)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(rc => rc.Status)
            .HasConversion<string>()
            .HasMaxLength(15);

        builder.Property(rc => rc.IsActive).HasDefaultValue(true);

        builder.HasIndex(rc => new { rc.Status, rc.IsActive, rc.CreatedAt })
            .IsDescending(false, false, true);

        builder.HasOne(rc => rc.Reporter)
            .WithMany(u => u.ReportedCases)
            .HasForeignKey(rc => rc.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rc => rc.AssignedFoster)
            .WithMany()
            .HasForeignKey(rc => rc.AssignedFosterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(rc => rc.UrgencyOverriddenBy)
            .WithMany()
            .HasForeignKey(rc => rc.UrgencyOverriddenById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

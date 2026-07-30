using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(n => n.Type).IsRequired().HasMaxLength(100);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(500);
        builder.Property(n => n.Body).IsRequired();
        builder.Property(n => n.ReferenceType).HasMaxLength(100);
        builder.Property(n => n.IsRead).HasDefaultValue(false);

        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
            .IsDescending(false, false, true);

        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

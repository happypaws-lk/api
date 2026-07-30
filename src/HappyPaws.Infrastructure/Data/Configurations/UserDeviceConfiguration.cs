using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> builder)
    {
        builder.ToTable("user_devices");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.FcmToken).IsRequired().HasMaxLength(500);
        builder.HasIndex(d => d.FcmToken).IsUnique();

        builder.Property(d => d.DeviceName).HasMaxLength(200);

        builder.Property(d => d.Platform)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.HasOne(d => d.User)
            .WithMany(u => u.Devices)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

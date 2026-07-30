using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("otp_codes");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(o => o.Code).IsRequired().HasMaxLength(10);
        builder.Property(o => o.IsUsed).HasDefaultValue(false);

        builder.HasIndex(o => new { o.UserId, o.IsUsed, o.ExpiresAt });

        builder.HasOne(o => o.User)
            .WithMany(u => u.OtpCodes)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

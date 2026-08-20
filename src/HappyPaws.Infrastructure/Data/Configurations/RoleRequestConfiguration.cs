using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class RoleRequestConfiguration : IEntityTypeConfiguration<RoleRequest>
{
    public void Configure(EntityTypeBuilder<RoleRequest> builder)
    {
        builder.ToTable("role_requests");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.DocumentKey).IsRequired().HasMaxLength(500);

        builder.Property(r => r.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.DocumentType)
            .HasConversion<string>()
            .HasMaxLength(15);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(r => r.Justification).HasMaxLength(300);
        builder.Property(r => r.RejectionReason).HasMaxLength(500);

        builder.HasOne(r => r.User)
            .WithMany(u => u.RoleRequests)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ReviewedBy)
            .WithMany()
            .HasForeignKey(r => r.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

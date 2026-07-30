using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(ur => ur.Id);
        builder.Property(ur => ur.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(ur => ur.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(ur => new { ur.UserId, ur.Role }).IsUnique();

        builder.HasOne(ur => ur.User)
            .WithMany(u => u.Roles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

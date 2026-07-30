using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class CaseUpdateConfiguration : IEntityTypeConfiguration<CaseUpdate>
{
    public void Configure(EntityTypeBuilder<CaseUpdate> builder)
    {
        builder.ToTable("case_updates");

        builder.HasKey(cu => cu.Id);
        builder.Property(cu => cu.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(cu => cu.UpdateType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(cu => cu.UpdateText).IsRequired();
        builder.Property(cu => cu.PhotoKey).HasMaxLength(500);

        builder.HasOne(cu => cu.Case)
            .WithMany(rc => rc.CaseUpdates)
            .HasForeignKey(cu => cu.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cu => cu.User)
            .WithMany()
            .HasForeignKey(cu => cu.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

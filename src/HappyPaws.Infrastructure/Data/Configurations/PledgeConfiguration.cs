using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class PledgeConfiguration : IEntityTypeConfiguration<Pledge>
{
    public void Configure(EntityTypeBuilder<Pledge> builder)
    {
        builder.ToTable("pledges", t =>
        {
            t.HasCheckConstraint("CK_Pledge_HasTarget",
                "\"CaseId\" IS NOT NULL OR \"ListingId\" IS NOT NULL");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Amount).HasPrecision(18, 2);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(p => p.Note).HasMaxLength(1000);

        builder.HasOne(p => p.Sponsor)
            .WithMany(u => u.Pledges)
            .HasForeignKey(p => p.SponsorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Case)
            .WithMany(rc => rc.Pledges)
            .HasForeignKey(p => p.CaseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.Listing)
            .WithMany(al => al.Pledges)
            .HasForeignKey(p => p.ListingId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

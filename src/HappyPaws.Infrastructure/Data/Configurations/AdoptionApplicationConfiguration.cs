using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class AdoptionApplicationConfiguration : IEntityTypeConfiguration<AdoptionApplication>
{
    public void Configure(EntityTypeBuilder<AdoptionApplication> builder)
    {
        builder.ToTable("adoption_applications");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(a => a.ReviewNotes).HasMaxLength(2000);

        builder.HasIndex(a => new { a.ListingId, a.ApplicantId }).IsUnique();
        builder.HasIndex(a => new { a.ListingId, a.Status });

        builder.HasOne(a => a.Listing)
            .WithMany(al => al.Applications)
            .HasForeignKey(a => a.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Applicant)
            .WithMany(u => u.Applications)
            .HasForeignKey(a => a.ApplicantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

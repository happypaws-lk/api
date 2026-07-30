using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class ListingPhotoConfiguration : IEntityTypeConfiguration<ListingPhoto>
{
    public void Configure(EntityTypeBuilder<ListingPhoto> builder)
    {
        builder.ToTable("listing_photos");

        builder.HasKey(lp => lp.Id);
        builder.Property(lp => lp.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(lp => lp.StorageKey).IsRequired().HasMaxLength(500);
        builder.Property(lp => lp.SortOrder).HasDefaultValue(0);

        builder.HasIndex(lp => new { lp.ListingId, lp.SortOrder });

        builder.HasOne(lp => lp.Listing)
            .WithMany(al => al.Photos)
            .HasForeignKey(lp => lp.ListingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

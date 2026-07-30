using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class AnimalListingConfiguration : IEntityTypeConfiguration<AnimalListing>
{
    public void Configure(EntityTypeBuilder<AnimalListing> builder)
    {
        builder.ToTable("animal_listings");

        builder.HasKey(al => al.Id);
        builder.Property(al => al.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(al => al.Name).IsRequired().HasMaxLength(200);
        builder.Property(al => al.Species).IsRequired().HasMaxLength(100);
        builder.Property(al => al.Breed).IsRequired().HasMaxLength(100);
        builder.Property(al => al.AgeLabel).HasMaxLength(50);
        builder.Property(al => al.Description).IsRequired();
        builder.Property(al => al.LocationName).IsRequired().HasMaxLength(500);

        builder.Property(al => al.LocationCoords)
            .HasColumnType("geography (point, 4326)");
        builder.HasIndex(al => al.LocationCoords).HasMethod("gist");

        builder.Property(al => al.Gender)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(al => al.Size)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(al => al.ActivityLevel)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(al => al.Status)
            .HasConversion<string>()
            .HasMaxLength(15);

        builder.Property(al => al.IsActive).HasDefaultValue(true);

        builder.HasIndex(al => new { al.Status, al.IsActive, al.CreatedAt })
            .IsDescending(false, false, true);

        builder.HasOne(al => al.Owner)
            .WithMany(u => u.Listings)
            .HasForeignKey(al => al.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(al => al.RescueCase)
            .WithMany(rc => rc.AnimalListings)
            .HasForeignKey(al => al.RescueCaseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

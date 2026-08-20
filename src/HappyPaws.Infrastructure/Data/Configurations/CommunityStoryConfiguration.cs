using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class CommunityStoryConfiguration : IEntityTypeConfiguration<CommunityStory>
{
    public void Configure(EntityTypeBuilder<CommunityStory> builder)
    {
        builder.ToTable("community_stories");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Content)
            .IsRequired();

        builder.Property(s => s.Tags)
            .HasColumnType("text[]");

        builder.HasOne(s => s.Author)
            .WithMany()
            .HasForeignKey(s => s.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

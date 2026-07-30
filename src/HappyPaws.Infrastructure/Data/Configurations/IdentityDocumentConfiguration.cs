using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class IdentityDocumentConfiguration : IEntityTypeConfiguration<IdentityDocument>
{
    public void Configure(EntityTypeBuilder<IdentityDocument> builder)
    {
        builder.ToTable("identity_documents");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.DocumentKey).IsRequired().HasMaxLength(500);

        builder.Property(d => d.DocumentType)
            .HasConversion<string>()
            .HasMaxLength(15);

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(d => d.RejectionReason).HasMaxLength(1000);

        builder.HasOne(d => d.User)
            .WithMany(u => u.IdentityDocuments)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ReviewedBy)
            .WithMany()
            .HasForeignKey(d => d.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

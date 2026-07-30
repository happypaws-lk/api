using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations", t =>
        {
            t.HasCheckConstraint("CK_Conversation_MutualExclusion",
                "NOT(\"ListingId\" IS NOT NULL AND \"CaseId\" IS NOT NULL)");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasOne(c => c.Listing)
            .WithMany(al => al.Conversations)
            .HasForeignKey(c => c.ListingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.Case)
            .WithMany(rc => rc.Conversations)
            .HasForeignKey(c => c.CaseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

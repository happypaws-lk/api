using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.ToTable("conversation_participants");

        builder.HasKey(cp => cp.Id);
        builder.Property(cp => cp.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasIndex(cp => new { cp.ConversationId, cp.UserId }).IsUnique();

        builder.HasOne(cp => cp.Conversation)
            .WithMany(c => c.Participants)
            .HasForeignKey(cp => cp.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.User)
            .WithMany(u => u.ConversationParticipants)
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.LastReadMessage)
            .WithMany()
            .HasForeignKey(cp => cp.LastReadMessageId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

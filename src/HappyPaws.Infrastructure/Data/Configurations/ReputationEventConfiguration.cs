using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class ReputationEventConfiguration : IEntityTypeConfiguration<ReputationEvent>
{
    public void Configure(EntityTypeBuilder<ReputationEvent> builder)
    {
        builder.ToTable("reputation_events");

        builder.HasKey(re => re.Id);
        builder.Property(re => re.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(re => re.EventType).IsRequired().HasMaxLength(100);
        builder.Property(re => re.ReferenceType).HasMaxLength(100);

        builder.HasOne(re => re.User)
            .WithMany(u => u.ReputationEvents)
            .HasForeignKey(re => re.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

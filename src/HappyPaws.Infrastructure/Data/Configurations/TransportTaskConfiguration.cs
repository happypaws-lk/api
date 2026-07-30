using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class TransportTaskConfiguration : IEntityTypeConfiguration<TransportTask>
{
    public void Configure(EntityTypeBuilder<TransportTask> builder)
    {
        builder.ToTable("transport_tasks");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.PickupLocationCoords)
            .HasColumnType("geography (point, 4326)");
        builder.HasIndex(t => t.PickupLocationCoords).HasMethod("gist");

        builder.Property(t => t.DropoffLocationCoords)
            .HasColumnType("geography (point, 4326)");
        builder.HasIndex(t => t.DropoffLocationCoords).HasMethod("gist");

        builder.Property(t => t.PickupLocation).IsRequired().HasMaxLength(500);
        builder.Property(t => t.DropoffLocation).IsRequired().HasMaxLength(500);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(15);

        builder.HasOne(t => t.Case)
            .WithMany(rc => rc.TransportTasks)
            .HasForeignKey(t => t.CaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Transporter)
            .WithMany(u => u.TransportTasks)
            .HasForeignKey(t => t.TransporterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

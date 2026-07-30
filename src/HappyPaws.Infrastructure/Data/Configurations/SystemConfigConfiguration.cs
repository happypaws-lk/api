using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Data.Configurations;

public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> builder)
    {
        builder.ToTable("system_configs", t =>
        {
            t.HasCheckConstraint("CK_SystemConfig_Singleton", "\"Id\" = 1");
        });

        builder.HasKey(sc => sc.Id);
        builder.Property(sc => sc.Id).ValueGeneratedNever();

        builder.HasData(new SystemConfig
        {
            Id = 1,
            AlertRadiusKm = 10,
            UpdatedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });
    }
}

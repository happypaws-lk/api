using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace HappyPaws.Infrastructure.Services;

public sealed class SystemConfigService(
    HappyPawsDbContext db,
    IMemoryCache cache,
    IConfiguration configuration) : ISystemConfigService
{
    private const string CacheKey = "sys:alert_radius_km";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<int> GetAlertRadiusKmAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out int cached))
            return cached;

        var config = await db.SystemConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var radius = config?.AlertRadiusKm
                     ?? configuration.GetValue<int>("SystemConfig:AlertRadiusKm", 10);

        cache.Set(CacheKey, radius, new MemoryCacheEntryOptions
        {
            SlidingExpiration = CacheDuration
        });

        return radius;
    }
}

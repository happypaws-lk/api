using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HappyPaws.Infrastructure.Data.Interceptors;

/// <summary>
/// EF Core interceptor that automatically stamps <c>CreatedAt</c> on new entities and <c>UpdatedAt</c> on every save.
/// </summary>
public sealed class TimestampInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Sets <c>CreatedAt</c> and <c>UpdatedAt</c> on added entities, and <c>UpdatedAt</c> on modified entities,
    /// using the current UTC time before changes are written to the database.
    /// </summary>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return ValueTask.FromResult(result);

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Properties.Any(p => p.Metadata.Name == "CreatedAt"))
                    entry.Property("CreatedAt").CurrentValue = now;

                if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                    entry.Property("UpdatedAt").CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                    entry.Property("UpdatedAt").CurrentValue = now;
            }
        }

        return ValueTask.FromResult(result);
    }
}

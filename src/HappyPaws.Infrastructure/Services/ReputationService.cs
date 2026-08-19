using HappyPaws.Core.Entities;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Services;

/// <summary>
/// Adds reputation points to a user and persists a <see cref="ReputationEvent"/> record for auditing.
/// </summary>
public sealed class ReputationService(HappyPawsDbContext dbContext) : IReputationService
{
    /// <summary>
    /// Increments the user's <c>ReputationPoints</c> by <paramref name="points"/> and writes a log entry.
    /// Does nothing if the user is not found.
    /// </summary>
    public async Task AwardPointsAsync(
        Guid userId,
        string eventType,
        int points,
        Guid? referenceId = null,
        string? referenceType = null,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return;

        user.ReputationPoints += points;

        var repEvent = new ReputationEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = eventType,
            Points = points,
            ReferenceId = referenceId,
            ReferenceType = referenceType
        };

        dbContext.ReputationEvents.Add(repEvent);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Interfaces;

/// <summary>
/// Manages user reputation points and logs reputation events for auditing.
/// </summary>
public interface IReputationService
{
    /// <summary>
    /// Awards reputation points to a user and logs the reputation event.
    /// </summary>
    Task AwardPointsAsync(
        Guid userId, 
        string eventType, 
        int points, 
        Guid? referenceId = null, 
        string? referenceType = null, 
        CancellationToken cancellationToken = default);
}

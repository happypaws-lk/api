using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Interfaces;

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

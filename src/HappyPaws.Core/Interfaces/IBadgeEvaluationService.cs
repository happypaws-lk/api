using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Interfaces;

public interface IBadgeEvaluationService
{
    /// <summary>
    /// Evaluates user actions and awards trust badges if criteria are met.
    /// </summary>
    Task EvaluateAndAwardBadgesAsync(Guid userId, CancellationToken cancellationToken = default);
}

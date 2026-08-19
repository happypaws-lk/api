using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Interfaces;

/// <summary>
/// Checks eligibility criteria and awards trust badges to users who have met the thresholds.
/// </summary>
public interface IBadgeEvaluationService
{
    /// <summary>
    /// Evaluates user actions and awards trust badges if criteria are met.
    /// </summary>
    Task EvaluateAndAwardBadgesAsync(Guid userId, CancellationToken cancellationToken = default);
}

using HappyPaws.Core.Enums;
using HappyPaws.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Services;

/// <summary>
/// Fallback urgency classifier that always returns <see cref="Urgency.Moderate"/> without inspecting the photo.
/// Used when the Gemini classifier is unavailable or times out.
/// </summary>
public sealed class RuleBasedUrgencyClassifier : IUrgencyClassifier
{
    private readonly ILogger<RuleBasedUrgencyClassifier> _logger;

    public RuleBasedUrgencyClassifier(ILogger<RuleBasedUrgencyClassifier> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns <see cref="Urgency.Moderate"/> immediately and logs a warning that the fallback was invoked.
    /// </summary>
    public Task<Urgency> ClassifyAsync(Stream photo, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Rule-based fallback classifier invoked — returning Moderate as default");
        return Task.FromResult(Urgency.Moderate);
    }
}

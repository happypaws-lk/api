using HappyPaws.Core.Enums;
using HappyPaws.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Services;

/// <summary>
/// Wraps the Gemini classifier with a timeout and a rule-based fallback.
/// If Gemini times out or throws, the service rewinds the stream and delegates to the rule-based classifier.
/// </summary>
public sealed class ResilientUrgencyClassificationService(
    [FromKeyedServices("gemini")] IUrgencyClassifier gemini,
    [FromKeyedServices("ruleBased")] IUrgencyClassifier ruleBased,
    IConfiguration configuration,
    ILogger<ResilientUrgencyClassificationService> logger) : IUrgencyClassificationService
{
    /// <summary>
    /// Tries Gemini first with a configurable timeout. Falls back to the rule-based classifier on timeout or error.
    /// Returns a <see cref="UrgencyClassificationResult"/> that records which source produced the result.
    /// </summary>
    public async Task<UrgencyClassificationResult> ClassifyAsync(Stream photo, CancellationToken cancellationToken = default)
    {
        var timeoutSeconds = configuration.GetValue("Gemini:TimeoutSeconds", 10);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var urgency = await gemini.ClassifyAsync(photo, timeoutCts.Token).ConfigureAwait(false);
            return new UrgencyClassificationResult(urgency, UrgencySource.Gemini, urgency);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Gemini classifier timed out after {Timeout}s, falling back to rule-based", timeoutSeconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Gemini classifier failed, falling back to rule-based");
        }

        photo.Position = 0;
        var fallback = await ruleBased.ClassifyAsync(photo, cancellationToken).ConfigureAwait(false);
        return new UrgencyClassificationResult(fallback, UrgencySource.RuleBased, null);
    }
}

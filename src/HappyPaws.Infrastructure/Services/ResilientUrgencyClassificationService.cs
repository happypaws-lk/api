using HappyPaws.Core.Enums;
using HappyPaws.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Services;

public sealed class ResilientUrgencyClassificationService(
    [FromKeyedServices("gemini")] IUrgencyClassifier gemini,
    [FromKeyedServices("ruleBased")] IUrgencyClassifier ruleBased,
    IConfiguration configuration,
    ILogger<ResilientUrgencyClassificationService> logger) : IUrgencyClassificationService
{
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

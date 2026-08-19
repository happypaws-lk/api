using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Interfaces;

/// <summary>
/// The result of an urgency classification, including which source produced it and the raw AI output before any override.
/// </summary>
public record UrgencyClassificationResult(Urgency Urgency, UrgencySource Source, Urgency? OriginalAiUrgency);

/// <summary>
/// High-level service that classifies rescue urgency with built-in fallback handling.
/// </summary>
public interface IUrgencyClassificationService
{
    /// <summary>
    /// Classifies the photo and returns the urgency along with source metadata.
    /// </summary>
    Task<UrgencyClassificationResult> ClassifyAsync(Stream photo, CancellationToken cancellationToken = default);
}

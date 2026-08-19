using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Interfaces;

/// <summary>
/// Classifies the urgency of a rescue case from a photo stream.
/// Implementations include the Gemini Vision classifier and a rule-based fallback.
/// </summary>
public interface IUrgencyClassifier
{
    /// <summary>
    /// Analyses the photo and returns a <see cref="Urgency"/> level: Low, Moderate, or Critical.
    /// </summary>
    Task<Urgency> ClassifyAsync(Stream photo, CancellationToken cancellationToken = default);
}

using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Interfaces;

public record UrgencyClassificationResult(Urgency Urgency, UrgencySource Source, Urgency? OriginalAiUrgency);

public interface IUrgencyClassificationService
{
    Task<UrgencyClassificationResult> ClassifyAsync(Stream photo, CancellationToken cancellationToken = default);
}

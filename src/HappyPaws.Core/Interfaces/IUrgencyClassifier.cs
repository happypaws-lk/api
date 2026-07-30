using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Interfaces;

public interface IUrgencyClassifier
{
    Task<Urgency> ClassifyAsync(Stream photo, CancellationToken cancellationToken = default);
}

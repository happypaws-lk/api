using HappyPaws.Core.Enums;
using HappyPaws.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Services;

public sealed class RuleBasedUrgencyClassifier : IUrgencyClassifier
{
    private readonly ILogger<RuleBasedUrgencyClassifier> _logger;

    public RuleBasedUrgencyClassifier(ILogger<RuleBasedUrgencyClassifier> logger)
    {
        _logger = logger;
    }

    public Task<Urgency> ClassifyAsync(Stream photo, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Rule-based fallback classifier invoked — returning Moderate as default");
        return Task.FromResult(Urgency.Moderate);
    }
}

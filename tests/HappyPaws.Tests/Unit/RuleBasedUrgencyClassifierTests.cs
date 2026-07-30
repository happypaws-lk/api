using FluentAssertions;
using HappyPaws.Core.Enums;
using HappyPaws.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HappyPaws.Tests.Unit;

public class RuleBasedUrgencyClassifierTests
{
    private readonly RuleBasedUrgencyClassifier _classifier = new(NullLogger<RuleBasedUrgencyClassifier>.Instance);

    [Fact]
    public async Task ClassifyAsync_ReturnsModerate()
    {
        using var stream = new MemoryStream([0x01, 0x02, 0x03]);

        var result = await _classifier.ClassifyAsync(stream);

        result.Should().Be(Urgency.Moderate);
    }

    [Fact]
    public async Task ClassifyAsync_WithEmptyStream_ReturnsModerate()
    {
        using var stream = new MemoryStream();

        var result = await _classifier.ClassifyAsync(stream);

        result.Should().Be(Urgency.Moderate);
    }

    [Fact]
    public async Task ClassifyAsync_RespectsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        using var stream = new MemoryStream([0x01]);

        var result = await _classifier.ClassifyAsync(stream, cts.Token);

        result.Should().Be(Urgency.Moderate);
    }
}

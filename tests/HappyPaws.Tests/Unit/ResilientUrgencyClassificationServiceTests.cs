using FluentAssertions;
using HappyPaws.Core.Enums;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace HappyPaws.Tests.Unit;

public class ResilientUrgencyClassificationServiceTests
{
    private readonly IUrgencyClassifier _gemini;
    private readonly IUrgencyClassifier _ruleBased;
    private readonly ResilientUrgencyClassificationService _service;

    public ResilientUrgencyClassificationServiceTests()
    {
        _gemini = Substitute.For<IUrgencyClassifier>();
        _ruleBased = Substitute.For<IUrgencyClassifier>();
        _ruleBased.ClassifyAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Urgency.Moderate);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:TimeoutSeconds"] = "5"
            })
            .Build();

        _service = new ResilientUrgencyClassificationService(
            _gemini,
            _ruleBased,
            config,
            NullLogger<ResilientUrgencyClassificationService>.Instance);
    }

    [Fact]
    public async Task ClassifyAsync_GeminiSucceeds_ReturnsGeminiResult()
    {
        using var stream = new MemoryStream([0x01]);
        _gemini.ClassifyAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Urgency.Critical);

        var result = await _service.ClassifyAsync(stream);

        result.Urgency.Should().Be(Urgency.Critical);
        result.Source.Should().Be(UrgencySource.Gemini);
        result.OriginalAiUrgency.Should().Be(Urgency.Critical);
    }

    [Fact]
    public async Task ClassifyAsync_GeminiThrowsHttpException_FallsBackToRuleBased()
    {
        using var stream = new MemoryStream([0x01]);
        _gemini.ClassifyAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var result = await _service.ClassifyAsync(stream);

        result.Urgency.Should().Be(Urgency.Moderate);
        result.Source.Should().Be(UrgencySource.RuleBased);
        result.OriginalAiUrgency.Should().BeNull();
    }

    [Fact]
    public async Task ClassifyAsync_GeminiThrowsInvalidOperation_FallsBackToRuleBased()
    {
        using var stream = new MemoryStream([0x01]);
        _gemini.ClassifyAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Unparseable response"));

        var result = await _service.ClassifyAsync(stream);

        result.Urgency.Should().Be(Urgency.Moderate);
        result.Source.Should().Be(UrgencySource.RuleBased);
        result.OriginalAiUrgency.Should().BeNull();
    }

    [Fact]
    public async Task ClassifyAsync_CallerCancels_DoesNotFallBack()
    {
        using var stream = new MemoryStream([0x01]);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _gemini.ClassifyAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var act = () => _service.ClassifyAsync(stream, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

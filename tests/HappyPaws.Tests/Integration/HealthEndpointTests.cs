using System.Net;
using FluentAssertions;

namespace HappyPaws.Tests.Integration;

[Collection("Integration")]
public class HealthEndpointTests
{
    private readonly HttpClient _client;

    public HealthEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/healthz")]
    public async Task HealthCheck_ReturnsOk(string route)
    {
        var response = await _client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("api.happypaws.lk is up and wagging its tail! Ready to connect paws with loving homes.");
    }
}

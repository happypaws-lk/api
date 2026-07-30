using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Core.Enums;
using HappyPaws.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Services;

public sealed class GeminiUrgencyClassifier(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<GeminiUrgencyClassifier> logger) : IUrgencyClassifier
{
    public async Task<Urgency> ClassifyAsync(Stream photo, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["Gemini:ApiKey"]
                     ?? throw new InvalidOperationException("Gemini:ApiKey is not configured");

        var model = configuration["Gemini:Model"] ?? "gemini-2.0-flash";

        using var memoryStream = new MemoryStream();
        await photo.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
        var base64Image = Convert.ToBase64String(memoryStream.ToArray());

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new
                        {
                            inline_data = new
                            {
                                mime_type = "image/jpeg",
                                data = base64Image
                            }
                        },
                        new
                        {
                            text = "Analyze this photo of an animal that may need rescue. Classify the urgency as exactly one of: Low, Moderate, Critical. Respond with only the single word."
                        }
                    }
                }
            }
        };

        var client = httpClientFactory.CreateClient("Gemini");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var response = await client.PostAsJsonAsync(url, requestBody, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken).ConfigureAwait(false);

        var text = json
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString()
            ?.Trim();

        if (Enum.TryParse<Urgency>(text, ignoreCase: true, out var urgency))
        {
            logger.LogInformation("Gemini classified urgency as {Urgency}", urgency);
            return urgency;
        }

        throw new InvalidOperationException($"Gemini returned unparseable urgency: '{text}'");
    }
}

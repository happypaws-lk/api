using HappyPaws.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Services;

/// <summary>
/// Development stub that stores files on the local filesystem under <c>wwwroot/dev-uploads</c>.
/// </summary>
public sealed class LocalStorageService : IStorageService
{
    private readonly ILogger<LocalStorageService> _logger;
    private readonly string _basePath;

    public LocalStorageService(ILogger<LocalStorageService> logger)
    {
        _logger = logger;
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "dev-uploads");
        Directory.CreateDirectory(_basePath);
    }

    /// <summary>
    /// Writes the stream to a local file path derived from <paramref name="key"/>. Returns the key.
    /// </summary>
    public async Task<string> UploadAsync(string key, Stream content, string contentType, bool isPrivate = false, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(directory);

        await using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation("Stored file locally: {Key}", key);
        return key;
    }

    /// <summary>
    /// Deletes the local file for the given key if it exists.
    /// </summary>
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Deleted local file: {Key}", key);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns a relative URL under <c>/dev-uploads</c> for use in local development.
    /// </summary>
    public string GetPublicUrl(string key)
    {
        return $"/dev-uploads/{key}";
    }

    /// <summary>
    /// Returns the same relative URL as <see cref="GetPublicUrl"/> — no real pre-signing occurs in development.
    /// </summary>
    public Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"/dev-uploads/{key}");
    }
}

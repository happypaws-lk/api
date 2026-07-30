using HappyPaws.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Services;

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

    public string GetPublicUrl(string key)
    {
        return $"/dev-uploads/{key}";
    }

    public Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"/dev-uploads/{key}");
    }
}

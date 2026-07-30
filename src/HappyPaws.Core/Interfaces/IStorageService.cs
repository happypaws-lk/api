namespace HappyPaws.Core.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(string key, Stream content, string contentType, bool isPrivate = false, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    string GetPublicUrl(string key);
    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);
}

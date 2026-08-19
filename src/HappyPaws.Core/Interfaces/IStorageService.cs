namespace HappyPaws.Core.Interfaces;

/// <summary>
/// Abstracts file storage operations so the application can swap between S3-compatible storage and local disk.
/// </summary>
public interface IStorageService
{
    /// <summary>Uploads a stream and returns the storage key.</summary>
    Task<string> UploadAsync(string key, Stream content, string contentType, bool isPrivate = false, CancellationToken cancellationToken = default);

    /// <summary>Deletes the object identified by <paramref name="key"/>.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns the publicly accessible URL for a non-private object.</summary>
    string GetPublicUrl(string key);

    /// <summary>Returns a time-limited URL for accessing a private object.</summary>
    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);
}

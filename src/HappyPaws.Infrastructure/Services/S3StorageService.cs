using Amazon.S3;
using Amazon.S3.Model;
using HappyPaws.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Services;

public sealed class S3StorageService : IStorageService, IDisposable
{
    private readonly AmazonS3Client _s3Client;
    private readonly ILogger<S3StorageService> _logger;
    private readonly string _publicBucket;
    private readonly string _privateBucket;
    private readonly string _publicBaseUrl;
    private readonly string? _serviceUrl;

    public S3StorageService(IConfiguration configuration, ILogger<S3StorageService> logger)
    {
        _logger = logger;

        _serviceUrl = configuration["Storage:ServiceUrl"];
        var accountId = configuration["Storage:AccountId"];
        var accessKey = configuration["Storage:AccessKey"];
        var secretKey = configuration["Storage:SecretKey"];
        var customDomain = configuration["Storage:CustomDomain"] ?? "cdn.happypaws.lk";

        _publicBucket = configuration["Storage:PublicBucket"] ?? throw new ArgumentException("Storage:PublicBucket config missing");
        _privateBucket = configuration["Storage:PrivateBucket"] ?? throw new ArgumentException("Storage:PrivateBucket config missing");
        _publicBaseUrl = configuration["Storage:PublicBaseUrl"] ?? $"https://{customDomain}";

        var config = new AmazonS3Config
        {
            ServiceURL = !string.IsNullOrEmpty(_serviceUrl)
                ? _serviceUrl
                : $"https://{accountId}.r2.cloudflarestorage.com",
            ForcePathStyle = !string.IsNullOrEmpty(_serviceUrl)
        };

        _s3Client = !string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey)
            ? new AmazonS3Client(accessKey, secretKey, config)
            : new AmazonS3Client(config);
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, bool isPrivate = false, CancellationToken cancellationToken = default)
    {
        var bucket = isPrivate ? _privateBucket : _publicBucket;

        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true
        };

        await _s3Client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Uploaded file {Key} to S3-compatible storage bucket {Bucket}", key, bucket);

        return key;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        // KYC docs live in the private bucket; all other keys belong to the public bucket.
        var bucket = key.StartsWith("kyc/", StringComparison.OrdinalIgnoreCase)
            ? _privateBucket
            : _publicBucket;

        var request = new DeleteObjectRequest
        {
            BucketName = bucket,
            Key = key
        };

        await _s3Client.DeleteObjectAsync(request, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Deleted file {Key} from S3-compatible storage bucket {Bucket}", key, bucket);
    }

    public string GetPublicUrl(string key)
    {
        return $"{_publicBaseUrl}/{key}";
    }

    public Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var protocol = !string.IsNullOrEmpty(_serviceUrl) && _serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            ? Protocol.HTTP
            : Protocol.HTTPS;

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _privateBucket,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiry),
            Protocol = protocol
        };

        var url = _s3Client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    public void Dispose() => _s3Client.Dispose();
}

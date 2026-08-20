using Amazon.S3;
using Amazon.S3.Model;
using HappyPaws.Api.Endpoints.Auth;
using HappyPaws.Core.Enums;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;

namespace HappyPaws.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public FakeEmailSender EmailSender { get; } = new FakeEmailSender();
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgis/postgis:16-3.4")
        .WithDatabase("happypaws_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private readonly MinioContainer _minio = new MinioBuilder("minio/minio:latest")
        .WithUsername("minioadmin")
        .WithPassword("minioadmin")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("RateLimiting:Disabled", "true");

        // Provide a deterministic JWT key for tests. Locally this comes from
        // User Secrets, but CI runners don't have secrets configured.
        builder.UseSetting("Jwt:Key", "integration-test-signing-key-that-is-long-enough-for-hmac-sha256!");


        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<HappyPawsDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<HappyPawsDbContext>(options =>
            {
                options.UseNpgsql(_postgres.GetConnectionString(),
                    npgsql => npgsql.UseNetTopologySuite());
            });

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            db.Database.Migrate();

            // Replace all IEmailSender registrations with a shared fake so tests
            // can retrieve OTP codes without relying on real email delivery.
            var emailDescriptors = services
                .Where(d => d.ServiceType == typeof(IEmailSender))
                .ToList();
            foreach (var d in emailDescriptors)
                services.Remove(d);

            services.AddSingleton<IEmailSender>(EmailSender);
        });

        builder.UseSetting("Storage:ServiceUrl", _minio.GetConnectionString());
        builder.UseSetting("Storage:AccessKey", "minioadmin");
        builder.UseSetting("Storage:SecretKey", "minioadmin");
        builder.UseSetting("Storage:PublicBucket", "happypaws-public");
        builder.UseSetting("Storage:PrivateBucket", "happypaws-private");
        builder.UseSetting("Storage:PublicBaseUrl", _minio.GetConnectionString() + "/happypaws-public");

        builder.UseEnvironment("Development");
    }

    /// <summary>
    /// Runs the three-step sign-up flow and returns the resulting auth tokens.
    /// </summary>
    public async Task<AuthResponse> SignupAsync(
        HttpClient client,
        string name,
        string email,
        string password,
        Role role = Role.Adopter)
    {
        await client.PostAsJsonAsync("/api/v1/auth/signup/send-code",
            new SignupSendCodeRequest(email));

        var otp = EmailSender.GetSignupOtp(email);

        var verifyResponse = await client.PostAsJsonAsync("/api/v1/auth/signup/verify-code",
            new SignupVerifyCodeRequest(email, otp));
        var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<SignupVerifyCodeResponse>(TestJsonOptions.Default);

        var completeResponse = await client.PostAsJsonAsync("/api/v1/auth/signup/complete",
            new SignupCompleteRequest(verifyResult!.SignupToken, name, password, role));

        return (await completeResponse.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Default))!;
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _minio.StartAsync());

        var config = new AmazonS3Config
        {
            ServiceURL = _minio.GetConnectionString(),
            ForcePathStyle = true
        };

        using var s3 = new AmazonS3Client("minioadmin", "minioadmin", config);

        await s3.PutBucketAsync(new PutBucketRequest { BucketName = "happypaws-public" });
        await s3.PutBucketAsync(new PutBucketRequest { BucketName = "happypaws-private" });

        var publicReadPolicy = """
            {
              "Version": "2012-10-17",
              "Statement": [
                {
                  "Effect": "Allow",
                  "Principal": {"AWS": ["*"]},
                  "Action": ["s3:GetObject"],
                  "Resource": ["arn:aws:s3:::happypaws-public/*"]
                }
              ]
            }
            """;

        await s3.PutBucketPolicyAsync(new PutBucketPolicyRequest
        {
            BucketName = "happypaws-public",
            Policy = publicReadPolicy
        });
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _minio.DisposeAsync();
        await base.DisposeAsync();
    }
}

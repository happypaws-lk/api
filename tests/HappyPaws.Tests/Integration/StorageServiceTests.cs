using FluentAssertions;
using HappyPaws.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Tests.Integration;

public class StorageServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StorageServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UploadAsync_PublicFile_ReturnsKey()
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();

        var key = $"test/{Guid.NewGuid()}.png";
        using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47]);

        var result = await storage.UploadAsync(key, stream, "image/png");

        result.Should().Be(key);
    }

    [Fact]
    public async Task GetPublicUrl_AfterUpload_ContainsBucketAndKey()
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();

        var key = $"test/{Guid.NewGuid()}.png";
        using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47]);
        await storage.UploadAsync(key, stream, "image/png");

        var url = storage.GetPublicUrl(key);

        url.Should().Contain("happypaws-public");
        url.Should().Contain(key);
    }

    [Fact]
    public async Task DeleteAsync_ExistingFile_DoesNotThrow()
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();

        var key = $"test/{Guid.NewGuid()}.png";
        using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47]);
        await storage.UploadAsync(key, stream, "image/png");

        var firstDelete = async () => await storage.DeleteAsync(key);
        var secondDelete = async () => await storage.DeleteAsync(key);

        await firstDelete.Should().NotThrowAsync();
        await secondDelete.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetPresignedUrlAsync_PrivateFile_ReturnsNonEmptyUrl()
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();

        var key = $"kyc/{Guid.NewGuid()}.pdf";
        using var stream = new MemoryStream([0x25, 0x50, 0x44, 0x46]);
        await storage.UploadAsync(key, stream, "application/pdf", isPrivate: true);

        var url = await storage.GetPresignedUrlAsync(key, TimeSpan.FromMinutes(5));

        url.Should().NotBeNullOrEmpty();
        url.Should().Contain(key);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HappyPaws.Api.Endpoints.Users;
using HappyPaws.Core.Enums;

namespace HappyPaws.Tests.Integration;

[Collection("Integration")]
public class UsersEndpointsTests
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public UsersEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DeleteAvatar_WhenNoAvatar_ReturnsNotFound()
    {
        var email = $"user-no-avatar-{Guid.NewGuid():N}@example.com";
        var auth = await _factory.SignupAsync(_client, "No Avatar User", email, "Password123!");

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/users/me/avatar");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAvatar_WhenAvatarExists_DeletesAndReturnsNoContent()
    {
        var email = $"user-with-avatar-{Guid.NewGuid():N}@example.com";
        var auth = await _factory.SignupAsync(_client, "Avatar User", email, "Password123!");

        // Upload avatar first
        using var uploadContent = new MultipartFormDataContent();
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        uploadContent.Add(fileContent, "file", "avatar.png");

        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/avatar")
        {
            Content = uploadContent
        };
        uploadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var uploadResponse = await _client.SendAsync(uploadRequest);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify profile has avatar
        using var getProfileRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        getProfileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var getProfileResponse = await _client.SendAsync(getProfileRequest);
        var profile = await getProfileResponse.Content.ReadFromJsonAsync<UserProfileResponse>();
        profile!.AvatarUrl.Should().NotBeNullOrEmpty();

        // Delete avatar
        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/users/me/avatar");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify profile avatar is now null
        using var getProfileAfterDelete = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        getProfileAfterDelete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var getProfileAfterDeleteResponse = await _client.SendAsync(getProfileAfterDelete);
        var updatedProfile = await getProfileAfterDeleteResponse.Content.ReadFromJsonAsync<UserProfileResponse>();
        updatedProfile!.AvatarUrl.Should().BeNull();
    }
}

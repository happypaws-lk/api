using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HappyPaws.Api.Endpoints.Auth;
using HappyPaws.Core.Enums;

namespace HappyPaws.Tests.Integration;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsCreatedWithTokens()
    {
        var request = new RegisterRequest("Test User", $"test{Guid.NewGuid():N}@example.com", "Password123!", Role.Adopter);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
        auth.RefreshToken.Should().NotBeNullOrEmpty();
        auth.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = $"dup{Guid.NewGuid():N}@example.com";
        var request = new RegisterRequest("User One", email, "Password123!", Role.Adopter);

        await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        var email = $"login{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("Login User", email, "Password123!", Role.Adopter));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var email = $"bad{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("Bad User", email, "Password123!", Role.Adopter));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "WrongPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        var email = $"refresh{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("Refresh User", email, "Password123!", Role.Adopter));
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(auth!.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAuth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        newAuth!.AccessToken.Should().NotBeNullOrEmpty();
        newAuth.RefreshToken.Should().NotBe(auth.RefreshToken);
    }

    [Fact]
    public async Task Refresh_RevokedToken_RevokesFamily()
    {
        var email = $"stolen{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("Stolen User", email, "Password123!", Role.Adopter));
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var originalRefreshToken = auth!.RefreshToken;

        // Use the refresh token once (rotates it)
        var firstRefresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(originalRefreshToken));
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);

        // Reuse the now-revoked token (should trigger family revocation)
        var secondRefresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(originalRefreshToken));
        secondRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // The new token from firstRefresh should also be revoked
        var newAuth = await firstRefresh.Content.ReadFromJsonAsync<AuthResponse>();
        var thirdRefresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(newAuth!.RefreshToken));
        thirdRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoke_ValidToken_ReturnsNoContent()
    {
        var email = $"revoke{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("Revoke User", email, "Password123!", Role.Adopter));
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/revoke", new RevokeRequest(auth.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Token should no longer work
        _client.DefaultRequestHeaders.Authorization = null;
        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(auth.RefreshToken));
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

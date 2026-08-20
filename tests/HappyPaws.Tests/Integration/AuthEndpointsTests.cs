using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HappyPaws.Api.Endpoints.Auth;
using HappyPaws.Core.Enums;

namespace HappyPaws.Tests.Integration;

[Collection("Integration")]
public class AuthEndpointsTests
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SignupFlow_ValidRequest_ReturnsCreatedWithTokens()
    {
        var email = $"signup{Guid.NewGuid():N}@example.com";

        var sendResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/signup/send-code", new SignupSendCodeRequest(email));
        sendResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var otp = _factory.EmailSender.GetSignupOtp(email);
        otp.Should().NotBeNullOrEmpty();

        var verifyResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/signup/verify-code", new SignupVerifyCodeRequest(email, otp));
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<SignupVerifyCodeResponse>(TestJsonOptions.Default);
        verifyResult!.SignupToken.Should().NotBeNullOrEmpty();

        var completeResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/signup/complete",
            new SignupCompleteRequest(verifyResult.SignupToken, "Test User", "Password123!"));
        completeResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var auth = await completeResponse.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Default);
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
        auth.RefreshToken.Should().NotBeNullOrEmpty();
        auth.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task SignupSendCode_DuplicateEmail_ReturnsConflict()
    {
        var email = $"dup{Guid.NewGuid():N}@example.com";

        await _factory.SignupAsync(_client, "User One", email, "Password123!");
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/signup/send-code", new SignupSendCodeRequest(email));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SignupVerifyCode_WrongOtp_ReturnsUnauthorized()
    {
        var email = $"wrong{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync(
            "/api/v1/auth/signup/send-code", new SignupSendCodeRequest(email));

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/signup/verify-code", new SignupVerifyCodeRequest(email, "000000"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SignupComplete_InvalidToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/signup/complete",
            new SignupCompleteRequest("not-a-valid-token", "Name", "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        var email = $"login{Guid.NewGuid():N}@example.com";
        await _factory.SignupAsync(_client, "Login User", email, "Password123!");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest(email, "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Default);
        auth!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var email = $"bad{Guid.NewGuid():N}@example.com";
        await _factory.SignupAsync(_client, "Bad User", email, "Password123!");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest(email, "WrongPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        var email = $"refresh{Guid.NewGuid():N}@example.com";
        var auth = await _factory.SignupAsync(_client, "Refresh User", email, "Password123!");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new RefreshRequest(auth.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAuth = await response.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Default);
        newAuth!.AccessToken.Should().NotBeNullOrEmpty();
        newAuth.RefreshToken.Should().NotBe(auth.RefreshToken);
    }

    [Fact]
    public async Task Refresh_RevokedToken_RevokesFamily()
    {
        var email = $"stolen{Guid.NewGuid():N}@example.com";
        var auth = await _factory.SignupAsync(_client, "Stolen User", email, "Password123!");
        var originalRefreshToken = auth.RefreshToken;

        // Use the refresh token once (rotates it)
        var firstRefresh = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new RefreshRequest(originalRefreshToken));
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);

        // Reuse the now-revoked token (should trigger family revocation)
        var secondRefresh = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new RefreshRequest(originalRefreshToken));
        secondRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // The new token from firstRefresh should also be revoked
        var newAuth = await firstRefresh.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Default);
        var thirdRefresh = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new RefreshRequest(newAuth!.RefreshToken));
        thirdRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoke_ValidToken_ReturnsNoContent()
    {
        var email = $"revoke{Guid.NewGuid():N}@example.com";
        var auth = await _factory.SignupAsync(_client, "Revoke User", email, "Password123!");

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/revoke", new RevokeRequest(auth.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        _client.DefaultRequestHeaders.Authorization = null;
        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new RefreshRequest(auth.RefreshToken));
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

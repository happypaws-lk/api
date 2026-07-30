using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HappyPaws.Api.Endpoints.Auth;
using HappyPaws.Api.Endpoints.Listings;
using HappyPaws.Api.Endpoints.Rescues;
using HappyPaws.Api.Endpoints.Transports;
using HappyPaws.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Tests.Integration;

public class FosterAndTransportFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public FosterAndTransportFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompleteFosterAndTransportFlow_Succeeds()
    {
        // 1. Register users
        var fosterToken = await RegisterAndVerifyUserAsync(Role.Foster);
        var transporterToken = await RegisterAndVerifyUserAsync(Role.Transporter);

        // 2. Foster creates rescue case
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fosterToken);
        var createRescueContent = CreateMultipartContent();
        var rescueResponse = await _client.PostAsync("/api/v1/rescues", createRescueContent);
        rescueResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var caseId = (await rescueResponse.Content.ReadFromJsonAsync<RescueCaseResponse>())!.Id;

        // 3. Foster accepts case
        var acceptResponse = await _client.PostAsync($"/api/v1/rescues/{caseId}/accept", null);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Foster creates transport task
        var transportReq = new CreateTransportRequest(
            caseId,
            6.9271, 79.8612, "Colombo Fort",
            6.8402, 79.8511, "Dehiwala Clinic"
        );
        var createTransportResponse = await _client.PostAsJsonAsync("/api/v1/transports", transportReq);
        createTransportResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var transportId = (await createTransportResponse.Content.ReadFromJsonAsync<TransportTaskResponse>())!.Id;

        // 5. Transporter claims task
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", transporterToken);
        var claimResponse = await _client.PostAsync($"/api/v1/transports/{transportId}/claim", null);
        claimResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. Transporter delivers animal
        var updateStatuses = new[] { TransportStatus.PickedUp, TransportStatus.InTransit, TransportStatus.Delivered };
        foreach (var status in updateStatuses)
        {
            var updateStatusResponse = await _client.PutAsJsonAsync(
                $"/api/v1/transports/{transportId}/status",
                new TransportStatusUpdateRequest(status));
            updateStatusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // 7. Foster resolves case
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fosterToken);
        var resolveResponse = await _client.PostAsync($"/api/v1/rescues/{caseId}/resolve", null);
        resolveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 8. Foster creates listing for adoption handoff
        var listingReq = new CreateListingRequest(
            caseId,
            "Rescued Puppy",
            "Dog",
            "Mixed",
            3,
            "Puppy",
            Gender.Male,
            AnimalSize.Small,
            ActivityLevel.High,
            "Looking for a forever home",
            6.8402, 79.8511, "Dehiwala Clinic"
        );
        var createListingResponse = await _client.PostAsJsonAsync("/api/v1/listings", listingReq);
        createListingResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static MultipartFormDataContent CreateMultipartContent()
    {
        var content = new MultipartFormDataContent();
        var photoBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        content.Add(new ByteArrayContent(photoBytes), "photo", "test.jpg");
        content.Add(new StringContent("6.9271"), "Latitude");
        content.Add(new StringContent("79.8612"), "Longitude");
        content.Add(new StringContent("Colombo Fort"), "LocationName");
        content.Add(new StringContent("Injured stray dog near train station"), "Description");
        return content;
    }

    private async Task<string> RegisterAndVerifyUserAsync(Role role)
    {
        var email = $"user{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest($"Test {role}", email, "Password123!", role));

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPaws.Infrastructure.Data.HappyPawsDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);
        user.IsVerified = true;
        await db.SaveChangesAsync();

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Password123!"));
        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        return loginAuth!.AccessToken;
    }
}

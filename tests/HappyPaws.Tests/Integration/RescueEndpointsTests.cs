using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HappyPaws.Api.Endpoints.Auth;
using HappyPaws.Api.Endpoints.Rescues;
using HappyPaws.Core.Common;
using HappyPaws.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Tests.Integration;

[Collection("Integration")]
public class RescueEndpointsTests
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public RescueEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateRescue_Unauthenticated_ReturnsUnauthorized()
    {
        var content = CreateMultipartContent();

        var response = await _client.PostAsync("/api/v1/rescues", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateRescue_VerifiedUser_ReturnsCreated()
    {
        var token = await RegisterAndVerifyUserAsync(Role.Foster);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = CreateMultipartContent();
        var response = await _client.PostAsync("/api/v1/rescues", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var rescue = await response.Content.ReadFromJsonAsync<RescueCaseResponse>(TestJsonOptions.Default);
        rescue.Should().NotBeNull();
        rescue!.LocationName.Should().Be("Colombo Fort");
        rescue.Urgency.Should().Be(Urgency.Moderate);
        rescue.UrgencySource.Should().Be(UrgencySource.RuleBased);
        rescue.Status.Should().Be(CaseStatus.PendingApproval);
    }

    [Fact]
    public async Task ListRescues_ReturnsPagedResults()
    {
        var token = await RegisterAndVerifyUserAsync(Role.Foster);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsync("/api/v1/rescues", CreateMultipartContent());
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/rescues?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<RescueCaseSummaryResponse>>(TestJsonOptions.Default);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ListRescues_FilterByUrgency_ReturnsFiltered()
    {
        var response = await _client.GetAsync("/api/v1/rescues?urgency=Critical");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRescue_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/rescues/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRescue_ExistingCase_ReturnsDetail()
    {
        var token = await RegisterAndVerifyUserAsync(Role.Foster);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var createResponse = await _client.PostAsync("/api/v1/rescues", CreateMultipartContent());
        var created = await createResponse.Content.ReadFromJsonAsync<RescueCaseResponse>(TestJsonOptions.Default);
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/v1/rescues/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rescue = await response.Content.ReadFromJsonAsync<RescueCaseResponse>(TestJsonOptions.Default);
        rescue!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task AcceptRescue_FosterRole_SetsInProgress()
    {
        var reporterToken = await RegisterAndVerifyUserAsync(Role.Foster);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reporterToken);
        var createResponse = await _client.PostAsync("/api/v1/rescues", CreateMultipartContent());
        var created = await createResponse.Content.ReadFromJsonAsync<RescueCaseResponse>(TestJsonOptions.Default);

        var adminToken = await RegisterAndVerifyUserAsync(Role.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.PostAsync($"/api/v1/admin/cases/{created!.Id}/approve", null);

        var fosterToken = await RegisterAndVerifyUserAsync(Role.Foster);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fosterToken);

        var response = await _client.PostAsync($"/api/v1/rescues/{created!.Id}/accept", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rescue = await response.Content.ReadFromJsonAsync<RescueCaseResponse>(TestJsonOptions.Default);
        rescue!.Status.Should().Be(CaseStatus.InProgress);
        rescue.AssignedFosterId.Should().NotBeNull();
    }

    [Fact]
    public async Task AcceptRescue_AlreadyAccepted_ReturnsConflict()
    {
        var reporterToken = await RegisterAndVerifyUserAsync(Role.Foster);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reporterToken);
        var createResponse = await _client.PostAsync("/api/v1/rescues", CreateMultipartContent());
        var created = await createResponse.Content.ReadFromJsonAsync<RescueCaseResponse>(TestJsonOptions.Default);

        var adminToken = await RegisterAndVerifyUserAsync(Role.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.PostAsync($"/api/v1/admin/cases/{created!.Id}/approve", null);

        var foster1Token = await RegisterAndVerifyUserAsync(Role.Foster);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", foster1Token);
        await _client.PostAsync($"/api/v1/rescues/{created!.Id}/accept", null);

        var foster2Token = await RegisterAndVerifyUserAsync(Role.Foster);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", foster2Token);
        var response = await _client.PostAsync($"/api/v1/rescues/{created.Id}/accept", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PostUpdate_InvolvedUser_ReturnsCreated()
    {
        var token = await RegisterAndVerifyUserAsync(Role.Foster);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var createResponse = await _client.PostAsync("/api/v1/rescues", CreateMultipartContent());
        var created = await createResponse.Content.ReadFromJsonAsync<RescueCaseResponse>(TestJsonOptions.Default);

        var updateContent = new MultipartFormDataContent();
        updateContent.Add(new StringContent("Note"), "UpdateType");
        updateContent.Add(new StringContent("Animal is being fed and resting"), "UpdateText");

        var response = await _client.PostAsync($"/api/v1/rescues/{created!.Id}/updates", updateContent);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var update = await response.Content.ReadFromJsonAsync<CaseUpdateResponse>(TestJsonOptions.Default);
        update!.UpdateType.Should().Be(UpdateType.Note);
        update.UpdateText.Should().Be("Animal is being fed and resting");
    }

    [Fact]
    public async Task PostUpdate_UninvolvedUser_ReturnsForbidden()
    {
        var reporterToken = await RegisterAndVerifyUserAsync(Role.Foster);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reporterToken);
        var createResponse = await _client.PostAsync("/api/v1/rescues", CreateMultipartContent());
        var created = await createResponse.Content.ReadFromJsonAsync<RescueCaseResponse>(TestJsonOptions.Default);

        var otherToken = await RegisterAndVerifyUserAsync(Role.Foster);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var updateContent = new MultipartFormDataContent();
        updateContent.Add(new StringContent("Note"), "UpdateType");
        updateContent.Add(new StringContent("Trying to post"), "UpdateText");

        var response = await _client.PostAsync($"/api/v1/rescues/{created!.Id}/updates", updateContent);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUpdates_ExistingCase_ReturnsTimeline()
    {
        var token = await RegisterAndVerifyUserAsync(Role.Foster);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var createResponse = await _client.PostAsync("/api/v1/rescues", CreateMultipartContent());
        var created = await createResponse.Content.ReadFromJsonAsync<RescueCaseResponse>(TestJsonOptions.Default);

        var updateContent = new MultipartFormDataContent();
        updateContent.Add(new StringContent("StatusUpdate"), "UpdateType");
        updateContent.Add(new StringContent("Condition stable"), "UpdateText");
        await _client.PostAsync($"/api/v1/rescues/{created!.Id}/updates", updateContent);

        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync($"/api/v1/rescues/{created.Id}/updates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updates = await response.Content.ReadFromJsonAsync<List<CaseUpdateResponse>>(TestJsonOptions.Default);
        updates.Should().HaveCount(1);
    }

    [Fact]
    public async Task OverrideUrgency_AdminRole_UpdatesUrgency()
    {
        var reporterToken = await RegisterAndVerifyUserAsync(Role.Foster);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reporterToken);
        var createResponse = await _client.PostAsync("/api/v1/rescues", CreateMultipartContent());
        var created = await createResponse.Content.ReadFromJsonAsync<RescueCaseResponse>(TestJsonOptions.Default);

        var adminToken = await RegisterAndVerifyUserAsync(Role.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/rescues/{created!.Id}/urgency",
            new OverrideUrgencyRequest(Urgency.Critical));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rescue = await response.Content.ReadFromJsonAsync<RescueCaseResponse>(TestJsonOptions.Default);
        rescue!.Urgency.Should().Be(Urgency.Critical);
        rescue.UrgencySource.Should().Be(UrgencySource.ManualOverride);
    }

    [Fact]
    public async Task OverrideUrgency_NonAdminNonVet_ReturnsForbidden()
    {
        var reporterToken = await RegisterAndVerifyUserAsync(Role.Foster);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reporterToken);
        var createResponse = await _client.PostAsync("/api/v1/rescues", CreateMultipartContent());
        var created = await createResponse.Content.ReadFromJsonAsync<RescueCaseResponse>(TestJsonOptions.Default);

        var fosterToken = await RegisterAndVerifyUserAsync(Role.Foster);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fosterToken);

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/rescues/{created!.Id}/urgency",
            new OverrideUrgencyRequest(Urgency.Critical));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static MultipartFormDataContent CreateMultipartContent()
    {
        var content = new MultipartFormDataContent();
        var photoBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        content.Add(new ByteArrayContent(photoBytes), "photo", "test.jpg");
        content.Add(new StringContent("Urgent Rescue Case"), "Title");
        content.Add(new StringContent("6.9271"), "Latitude");
        content.Add(new StringContent("79.8612"), "Longitude");
        content.Add(new StringContent("Colombo Fort"), "LocationName");
        content.Add(new StringContent("Injured stray dog near train station"), "Description");
        content.Add(new StringContent("Injured, Critical"), "Tags");
        return content;
    }

    private async Task<string> RegisterAndVerifyUserAsync(Role role)
    {
        var email = $"user{Guid.NewGuid():N}@example.com";
        // Always signup via API as Adopter (Admin/Vet are blocked by validation)
        await _factory.SignupAsync(_client, $"Test {role}", email, "Password123!", Role.Adopter);

        // Directly verify the user in the DB for testing, and add the requested role
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPaws.Infrastructure.Data.HappyPawsDbContext>();
        var user = await db.Users.Include(u => u.Roles).FirstAsync(u => u.Email == email);
        user.IsVerified = true;

        if (role != Role.Adopter)
        {
            user.Roles.Add(new HappyPaws.Core.Entities.UserRole { Role = role });
        }
        await db.SaveChangesAsync();

        // Re-login to get a token with IsVerified claim
        var loginResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Password123!"));
        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Default);

        return loginAuth!.AccessToken;
    }
}

using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HappyPaws.Api.Endpoints.Auth;
using HappyPaws.Api.Endpoints.Conversations;
using HappyPaws.Core.Common;
using HappyPaws.Core.Enums;
using Microsoft.AspNetCore.SignalR.Client;

namespace HappyPaws.Tests.Integration;

[Collection("Integration")]
public class ConversationEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ConversationEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string accessToken, Guid userId)> CreateUserAsync(string name)
    {
        var email = $"{name.ToLower().Replace(" ", "")}{Guid.NewGuid():N}@example.com";
        var auth = await _factory.SignupAsync(_client, name, email, "Password123!");
        
        // Approve KYC to get verified
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HappyPaws.Infrastructure.Data.HappyPawsDbContext>();
        var user = dbContext.Users.First(u => u.Email == email);
        user.IsVerified = true;
        await dbContext.SaveChangesAsync();

        // Login again to get a new token with IsVerified = true
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Password123!"));
        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        return (loginAuth!.AccessToken, user.Id);
    }

    [Fact]
    public async Task CreateConversation_ValidRequest_ReturnsCreated()
    {
        var (token1, userId1) = await CreateUserAsync("User One");
        var (token2, userId2) = await CreateUserAsync("User Two");

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);

        var request = new CreateConversationRequest(userId2, null, null);
        var response = await _client.PostAsJsonAsync("/api/v1/conversations", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var conversation = await response.Content.ReadFromJsonAsync<ConversationResponse>();
        conversation.Should().NotBeNull();
        conversation!.ParticipantId.Should().Be(userId2);
    }

    [Fact]
    public async Task CreateConversation_AlreadyExists_ReturnsConflict()
    {
        var (token1, userId1) = await CreateUserAsync("User Three");
        var (token2, userId2) = await CreateUserAsync("User Four");

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);

        var request = new CreateConversationRequest(userId2, null, null);
        await _client.PostAsJsonAsync("/api/v1/conversations", request);
        
        var response = await _client.PostAsJsonAsync("/api/v1/conversations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SignalR_SendMessage_PersistsAndRelays()
    {
        var (token1, userId1) = await CreateUserAsync("Signal Sender");
        var (token2, userId2) = await CreateUserAsync("Signal Recipient");

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);
        var request = new CreateConversationRequest(userId2, null, null);
        var convResponse = await _client.PostAsJsonAsync("/api/v1/conversations", request);
        var conversation = await convResponse.Content.ReadFromJsonAsync<ConversationResponse>();
        var convId = conversation!.Id;

        var handler = _factory.Server.CreateHandler();
        
        var connection1 = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/chat", o =>
            {
                o.HttpMessageHandlerFactory = _ => handler;
                o.AccessTokenProvider = () => Task.FromResult(token1)!;
            })
            .Build();

        var connection2 = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/chat", o =>
            {
                o.HttpMessageHandlerFactory = _ => handler;
                o.AccessTokenProvider = () => Task.FromResult(token2)!;
            })
            .Build();

        var messageReceivedTcs = new TaskCompletionSource<MessageResponse>();
        connection2.On<MessageResponse>("ReceiveMessage", message =>
        {
            messageReceivedTcs.TrySetResult(message);
        });

        await connection1.StartAsync();
        await connection2.StartAsync();

        await connection1.InvokeAsync("SendMessage", convId.ToString(), "Hello SignalR!");

        var receivedMessage = await messageReceivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        receivedMessage.Should().NotBeNull();
        receivedMessage.Content.Should().Be("Hello SignalR!");
        receivedMessage.SenderId.Should().Be(userId1);

        // Verify DB persistence
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);
        var messagesResponse = await _client.GetFromJsonAsync<PagedResult<MessageResponse>>($"/api/v1/conversations/{convId}/messages");
        messagesResponse!.Items.Should().Contain(m => m.Content == "Hello SignalR!");
        
        await connection1.StopAsync();
        await connection2.StopAsync();
    }
}

using System.Collections.Concurrent;
using System.Security.Claims;
using HappyPaws.Api.Endpoints.Conversations;
using HappyPaws.Core.Entities;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Api.Hubs;

/// <summary>
/// SignalR hub for real-time chat between users.
/// Tracks active connections in a static dictionary and adds each user to their conversation groups on connect.
/// </summary>
[Authorize]
public sealed class ChatHub(
    HappyPawsDbContext dbContext,
    INotificationService notificationService,
    ILogger<ChatHub> logger) : Hub<IChatClient>
{
    private static readonly ConcurrentDictionary<string, string> UserConnections = new();

    /// <summary>
    /// Registers the connection, adds the user to all their conversation groups, and cleans up on disconnect.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            UserConnections[userId] = Context.ConnectionId;

            var conversationIds = await dbContext.ConversationParticipants
                .Where(cp => cp.UserId == Guid.Parse(userId))
                .Select(cp => cp.ConversationId)
                .ToListAsync(Context.ConnectionAborted);

            foreach (var convId in conversationIds)
                await Groups.AddToGroupAsync(Context.ConnectionId, convId.ToString());
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Removes the user's connection entry from the tracking dictionary.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
            UserConnections.TryRemove(userId, out _);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Saves the message to the database, then delivers it directly to the recipient's connection if they are online.
    /// Falls back to a push notification when the recipient is offline.
    /// </summary>
    public async Task SendMessage(string conversationId, string content)
    {
        var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var senderId) || !Guid.TryParse(conversationId, out var convId))
            return;

        var participant = await dbContext.ConversationParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(cp => cp.ConversationId == convId && cp.UserId == senderId, Context.ConnectionAborted);

        if (participant is null)
        {
            logger.LogWarning("User {UserId} attempted to send message to conversation {ConversationId} but is not a participant.", senderId, convId);
            return;
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = convId,
            SenderId = senderId,
            Content = content,
            SentAt = DateTimeOffset.UtcNow
        };

        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(Context.ConnectionAborted);

        var messageDto = new MessageResponse(message.Id, message.SenderId, message.Content, message.SentAt);

        var recipientId = await dbContext.ConversationParticipants
            .AsNoTracking()
            .Where(cp => cp.ConversationId == convId && cp.UserId != senderId)
            .Select(cp => cp.UserId)
            .FirstOrDefaultAsync(Context.ConnectionAborted);

        if (recipientId != Guid.Empty)
        {
            if (UserConnections.TryGetValue(recipientId.ToString(), out var recipientConnectionId))
            {
                await Clients.Client(recipientConnectionId).ReceiveMessage(messageDto);
            }
            else
            {
                var senderName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Someone";
                await notificationService.SendNotificationAsync(
                    recipientId,
                    "NewMessage",
                    $"New message from {senderName}",
                    content.Length > 50 ? content[..47] + "..." : content,
                    referenceId: convId,
                    referenceType: "Conversation",
                    cancellationToken: default);
            }
        }

        await Clients.Caller.ReceiveMessage(messageDto);
    }
}

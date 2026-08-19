using HappyPaws.Api.Endpoints.Conversations;

namespace HappyPaws.Api.Hubs;

/// <summary>
/// Defines the client-side methods that <see cref="ChatHub"/> can invoke on connected SignalR clients.
/// </summary>
public interface IChatClient
{
    /// <summary>
    /// Pushes a new message to the client.
    /// </summary>
    Task ReceiveMessage(MessageResponse message);

    /// <summary>
    /// Notifies the client that a message in a conversation has been read by the specified user.
    /// </summary>
    Task MessageRead(Guid conversationId, Guid userId, Guid messageId);
}

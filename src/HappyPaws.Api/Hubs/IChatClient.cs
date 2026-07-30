using HappyPaws.Api.Endpoints.Conversations;

namespace HappyPaws.Api.Hubs;

public interface IChatClient
{
    Task ReceiveMessage(MessageResponse message);
    Task MessageRead(Guid conversationId, Guid userId, Guid messageId);
}

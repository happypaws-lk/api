namespace HappyPaws.Core.Entities;

public class ConversationParticipant
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? LastReadMessageId { get; set; }
    public DateTimeOffset? LastReadAt { get; set; }
    public DateTimeOffset JoinedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public User User { get; set; } = null!;
    public Message? LastReadMessage { get; set; }
}

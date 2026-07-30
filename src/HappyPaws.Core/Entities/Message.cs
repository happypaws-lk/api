namespace HappyPaws.Core.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = null!;
    public DateTimeOffset SentAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
}

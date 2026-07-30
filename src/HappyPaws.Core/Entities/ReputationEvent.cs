namespace HappyPaws.Core.Entities;

public class ReputationEvent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string EventType { get; set; } = null!;
    public int Points { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}

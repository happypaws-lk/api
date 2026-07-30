namespace HappyPaws.Core.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}

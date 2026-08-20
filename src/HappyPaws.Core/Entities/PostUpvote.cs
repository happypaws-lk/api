using System;

namespace HappyPaws.Core.Entities;

public class PostUpvote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TargetType { get; set; } = string.Empty; // e.g., "RescueCase", "AnimalListing"
    public Guid TargetId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
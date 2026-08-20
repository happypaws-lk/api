using System;
using System.Collections.Generic;

namespace HappyPaws.Core.Entities;

public class CommunityStory
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? PhotoKey { get; set; }
    public string? VideoKey { get; set; }
    public List<string> Tags { get; set; } = new();
    public Guid AuthorId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public User Author { get; set; } = null!;
}

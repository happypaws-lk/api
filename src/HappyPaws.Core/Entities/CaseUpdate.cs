using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Entities;

public class CaseUpdate
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public Guid UserId { get; set; }
    public UpdateType UpdateType { get; set; }
    public string UpdateText { get; set; } = null!;
    public string? PhotoKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public RescueCase Case { get; set; } = null!;
    public User User { get; set; } = null!;
}

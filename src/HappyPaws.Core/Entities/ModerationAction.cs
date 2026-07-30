using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Entities;

public class ModerationAction
{
    public Guid Id { get; set; }
    public Guid AdminId { get; set; }
    public ModerationTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public ModerationActionType ActionType { get; set; }
    public string Reason { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public User Admin { get; set; } = null!;
}

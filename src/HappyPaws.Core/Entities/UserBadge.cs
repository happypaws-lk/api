using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Entities;

public class UserBadge
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public BadgeType BadgeType { get; set; }
    public DateTimeOffset AwardedAt { get; set; }

    public User User { get; set; } = null!;
}

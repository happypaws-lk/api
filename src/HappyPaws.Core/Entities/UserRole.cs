using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Entities;

public class UserRole
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Role Role { get; set; }
    public DateTimeOffset AssignedAt { get; set; }

    public User User { get; set; } = null!;
}

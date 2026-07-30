using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Entities;

public class UserDevice
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FcmToken { get; set; } = null!;
    public string? DeviceName { get; set; }
    public Platform Platform { get; set; }
    public DateTimeOffset LastActiveAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}

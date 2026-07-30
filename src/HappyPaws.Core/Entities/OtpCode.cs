namespace HappyPaws.Core.Entities;

public class OtpCode
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Code { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}

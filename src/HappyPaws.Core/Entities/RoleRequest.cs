using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Entities;

public class RoleRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ReviewedById { get; set; }
    public Role Role { get; set; }
    public DocumentType DocumentType { get; set; }
    public string DocumentKey { get; set; } = null!;
    public RoleRequestStatus Status { get; set; }
    public string? Justification { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }

    public User User { get; set; } = null!;
    public User? ReviewedBy { get; set; }
}

using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Entities;

public class IdentityDocument
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ReviewedById { get; set; }
    public string DocumentKey { get; set; } = null!;
    public DocumentType DocumentType { get; set; }
    public DocumentStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }

    public User User { get; set; } = null!;
    public User? ReviewedBy { get; set; }
}

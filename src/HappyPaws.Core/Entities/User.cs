using NetTopologySuite.Geometries;

namespace HappyPaws.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? AvatarKey { get; set; }
    public Point? LastKnownLocation { get; set; }
    public bool IsVerified { get; set; }
    public int ReputationPoints { get; set; }
    public bool IsSuspended { get; set; }
    public DateTimeOffset? SuspendedAt { get; set; }
    public string? SuspendedReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<UserRole> Roles { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<OtpCode> OtpCodes { get; set; } = [];
    public LifestyleProfile? LifestyleProfile { get; set; }
    public ICollection<IdentityDocument> IdentityDocuments { get; set; } = [];
    public ICollection<RoleRequest> RoleRequests { get; set; } = [];
    public ICollection<RescueCase> ReportedCases { get; set; } = [];
    public ICollection<AnimalListing> Listings { get; set; } = [];
    public ICollection<AdoptionApplication> Applications { get; set; } = [];
    public ICollection<Pledge> Pledges { get; set; } = [];
    public ICollection<TransportTask> TransportTasks { get; set; } = [];
    public ICollection<UserDevice> Devices { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<ReputationEvent> ReputationEvents { get; set; } = [];
    public ICollection<UserBadge> Badges { get; set; } = [];
    public ICollection<ConversationParticipant> ConversationParticipants { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}

using HappyPaws.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Data;

/// <summary>
/// The main EF Core database context for HappyPaws. Applies all entity configurations from the Infrastructure assembly
/// and enables the PostGIS extension for geospatial queries.
/// </summary>
public sealed class HappyPawsDbContext : DbContext
{
    public HappyPawsDbContext(DbContextOptions<HappyPawsDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<LifestyleProfile> LifestyleProfiles => Set<LifestyleProfile>();
    public DbSet<IdentityDocument> IdentityDocuments => Set<IdentityDocument>();
    public DbSet<RoleRequest> RoleRequests => Set<RoleRequest>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<RescueCase> RescueCases => Set<RescueCase>();
    public DbSet<CaseUpdate> CaseUpdates => Set<CaseUpdate>();
    public DbSet<AnimalListing> AnimalListings => Set<AnimalListing>();
    public DbSet<ListingPhoto> ListingPhotos => Set<ListingPhoto>();
    public DbSet<AdoptionApplication> AdoptionApplications => Set<AdoptionApplication>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Pledge> Pledges => Set<Pledge>();
    public DbSet<TransportTask> TransportTasks => Set<TransportTask>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ReputationEvent> ReputationEvents => Set<ReputationEvent>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();
    public DbSet<ModerationAction> ModerationActions => Set<ModerationAction>();
    public DbSet<CommunityStory> CommunityStories => Set<CommunityStory>();
    public DbSet<PostUpvote> PostUpvotes => Set<PostUpvote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HappyPawsDbContext).Assembly);
    }
}

using HappyPaws.Core.Enums;

namespace HappyPaws.Api.Endpoints.Admin;

/// <summary>Data submitted when rejecting a KYC document.</summary>
/// <param name="Reason">The reason for rejection, shown to the user.</param>
public record KycRejectRequest(string Reason);

/// <summary>A pending KYC document awaiting admin review.</summary>
/// <param name="Id">Unique identifier of the document.</param>
/// <param name="UserId">ID of the user who submitted the document.</param>
/// <param name="UserName">Display name of the submitting user.</param>
/// <param name="UserEmail">Email address of the submitting user.</param>
/// <param name="DocumentType">The type of identity document submitted.</param>
/// <param name="DocumentUrl">A short-lived presigned URL (15 minutes) to view the document.</param>
/// <param name="UploadedAt">UTC timestamp when the document was uploaded.</param>
public record KycPendingResponse(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserEmail,
    DocumentType DocumentType,
    string DocumentUrl,
    DateTimeOffset UploadedAt);

/// <summary>Summary statistics and recent activity for the admin dashboard.</summary>
/// <param name="PendingKycCount">Number of KYC documents awaiting review.</param>
/// <param name="OpenRescueCasesCount">Number of rescue cases currently open.</param>
/// <param name="TotalUsersCount">Total number of registered users.</param>
/// <param name="RecentActivity">The 5 most recent moderation actions.</param>
/// <param name="UserGrowth">Daily user registration and verification counts, with running cumulative totals, bounded by the requested date range.</param>
/// <param name="AdoptionActivity">Daily adoption application submissions and listings moved to Adopted status, bounded by the requested date range.</param>
public record DashboardResponse(
    int PendingKycCount,
    int OpenRescueCasesCount,
    int TotalUsersCount,
    List<ModerationLogResponse> RecentActivity,
    List<UserGrowthDataPoint> UserGrowth,
    List<AdoptionActivityDataPoint> AdoptionActivity);

/// <summary>A single day's user registration and verification snapshot with running cumulative totals.</summary>
/// <param name="Date">The calendar date this data point represents (ISO 8601, YYYY-MM-DD).</param>
/// <param name="TotalUsers">Cumulative total of all registered users up to and including this date.</param>
/// <param name="NewUsers">Number of users who registered on this specific date.</param>
/// <param name="VerifiedUsers">Cumulative total of verified users (approved KYC) up to and including this date.</param>
public record UserGrowthDataPoint(
    DateOnly Date,
    int TotalUsers,
    int NewUsers,
    int VerifiedUsers);

/// <summary>A single day's adoption pipeline activity.</summary>
/// <param name="Date">The calendar date this data point represents (ISO 8601, YYYY-MM-DD).</param>
/// <param name="Applications">Number of adoption applications submitted on this date.</param>
/// <param name="Adoptions">Number of listings that moved to Adopted status on this date.</param>
public record AdoptionActivityDataPoint(
    DateOnly Date,
    int Applications,
    int Adoptions);

/// <summary>A rescue case entry for the admin live map.</summary>
/// <param name="Id">Unique identifier of the rescue case.</param>
/// <param name="Longitude">WGS84 longitude of the case location.</param>
/// <param name="Latitude">WGS84 latitude of the case location.</param>
/// <param name="LocationName">Human-readable location name.</param>
/// <param name="Urgency">The current urgency level as a string.</param>
/// <param name="Status">The current case status as a string.</param>
public record AdminCaseResponse(
    Guid Id,
    double Longitude,
    double Latitude,
    string LocationName,
    string Urgency,
    string Status);

/// <summary>A user entry in the admin user list.</summary>
/// <param name="Id">Unique identifier of the user.</param>
/// <param name="Name">The user's display name.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="IsVerified">Whether the user has completed KYC verification.</param>
/// <param name="IsSuspended">Whether the user's account is currently suspended.</param>
/// <param name="ReputationPoints">The user's total reputation score.</param>
/// <param name="Roles">List of roles assigned to the user.</param>
public record AdminUserResponse(
    Guid Id,
    string Name,
    string Email,
    bool IsVerified,
    bool IsSuspended,
    int ReputationPoints,
    List<string> Roles,
    DateTimeOffset CreatedAt);

/// <summary>Data submitted when suspending a user.</summary>
/// <param name="Reason">The reason for the suspension, recorded in the moderation log.</param>
public record SuspendRequest(string Reason);

/// <summary>Data submitted when creating a moderation action.</summary>
/// <param name="TargetType">The type of content being moderated (User, Listing, or Message).</param>
/// <param name="TargetId">ID of the content being moderated.</param>
/// <param name="ActionType">The action to apply (Suspended, Removed, or Warned).</param>
/// <param name="Reason">The reason for the action, recorded in the log and shown to the user for warnings.</param>
public record ModerationRequest(
    ModerationTargetType TargetType,
    Guid TargetId,
    ModerationActionType ActionType,
    string Reason);

/// <summary>A single entry in the moderation action log.</summary>
/// <param name="Id">Unique identifier of the moderation action.</param>
/// <param name="AdminId">ID of the admin who performed the action.</param>
/// <param name="TargetType">The type of content that was moderated.</param>
/// <param name="TargetId">ID of the moderated content.</param>
/// <param name="ActionType">The action that was applied.</param>
/// <param name="Reason">The stated reason for the action.</param>
/// <param name="CreatedAt">UTC timestamp when the action was recorded.</param>
public record ModerationLogResponse(
    Guid Id,
    Guid AdminId,
    string TargetType,
    Guid TargetId,
    string ActionType,
    string Reason,
    DateTimeOffset CreatedAt);

/// <summary>Data submitted when manually adjusting a user's reputation points.</summary>
/// <param name="PointsToAdjust">Points to add (positive) or deduct (negative) from the user's total.</param>
/// <param name="Reason">The reason for the adjustment, recorded in the reputation event log.</param>
public record ReputationAdjustRequest(
    int PointsToAdjust,
    string Reason);

/// <summary>An animal listing entry in the admin management list.</summary>
/// <param name="Id">Unique identifier of the listing.</param>
/// <param name="Name">The animal's name.</param>
/// <param name="Species">The animal's species.</param>
/// <param name="Breed">The animal's breed.</param>
/// <param name="OwnerId">ID of the user who created the listing.</param>
/// <param name="OwnerName">Display name of the listing owner.</param>
/// <param name="Status">The current adoption status.</param>
/// <param name="IsActive">Whether the listing is active (false means soft-deleted or moderated).</param>
/// <param name="LocationName">Human-readable location name.</param>
/// <param name="CreatedAt">UTC timestamp when the listing was created.</param>
/// <param name="UpdatedAt">UTC timestamp of the last update.</param>
public record AdminListingResponse(
    Guid Id,
    string Name,
    string Species,
    string Breed,
    Guid OwnerId,
    string OwnerName,
    ListingStatus Status,
    bool IsActive,
    string LocationName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Full details of a user for admin review.</summary>
/// <param name="Id">Unique identifier of the user.</param>
/// <param name="Name">The user's display name.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="IsVerified">Whether the user has completed KYC verification.</param>
/// <param name="IsSuspended">Whether the account is currently suspended.</param>
/// <param name="SuspendedAt">UTC timestamp when the account was suspended. Null if not suspended.</param>
/// <param name="SuspendedReason">The reason for suspension. Null if not suspended.</param>
/// <param name="ReputationPoints">The user's total reputation score.</param>
/// <param name="CreatedAt">UTC timestamp when the account was created.</param>
/// <param name="UpdatedAt">UTC timestamp when the account was last updated.</param>
/// <param name="Roles">All roles assigned to the user.</param>
/// <param name="Badges">All badges earned by the user.</param>
public record AdminUserDetailResponse(
    Guid Id,
    string Name,
    string Email,
    bool IsVerified,
    bool IsSuspended,
    DateTimeOffset? SuspendedAt,
    string? SuspendedReason,
    int ReputationPoints,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<string> Roles,
    List<string> Badges);

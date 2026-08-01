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
public record DashboardResponse(
    int PendingKycCount,
    int OpenRescueCasesCount,
    int TotalUsersCount,
    List<ModerationLogResponse> RecentActivity);

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
    List<string> Roles);

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

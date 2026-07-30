using HappyPaws.Core.Enums;

namespace HappyPaws.Api.Endpoints.Admin;

public record KycRejectRequest(string Reason);

public record KycPendingResponse(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserEmail,
    DocumentType DocumentType,
    string DocumentUrl,
    DateTimeOffset UploadedAt);

public record DashboardResponse(
    int PendingKycCount,
    int OpenRescueCasesCount,
    int TotalUsersCount,
    List<ModerationLogResponse> RecentActivity);

public record AdminCaseResponse(
    Guid Id,
    double Longitude,
    double Latitude,
    string LocationName,
    string Urgency,
    string Status);

public record AdminUserResponse(
    Guid Id,
    string Name,
    string Email,
    bool IsVerified,
    bool IsSuspended,
    int ReputationPoints,
    List<string> Roles);

public record SuspendRequest(string Reason);

public record ModerationRequest(
    ModerationTargetType TargetType,
    Guid TargetId,
    ModerationActionType ActionType,
    string Reason);

public record ModerationLogResponse(
    Guid Id,
    Guid AdminId,
    string TargetType,
    Guid TargetId,
    string ActionType,
    string Reason,
    DateTimeOffset CreatedAt);

public record ReputationAdjustRequest(
    int PointsToAdjust,
    string Reason);

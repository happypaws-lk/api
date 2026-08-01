namespace HappyPaws.Api.Endpoints.Notifications;

/// <summary>A notification delivered to a user.</summary>
/// <param name="Id">Unique identifier of the notification.</param>
/// <param name="Type">Machine-readable notification type (for example, "kyc_approved" or "rescue_nearby").</param>
/// <param name="Title">Short title shown in the notification.</param>
/// <param name="Body">Full notification message text.</param>
/// <param name="ReferenceId">ID of the related resource. Null if the notification has no linked resource.</param>
/// <param name="ReferenceType">Type name of the related resource (for example, "RescueCase"). Null if no linked resource.</param>
/// <param name="IsRead">Whether the user has read this notification.</param>
/// <param name="CreatedAt">UTC timestamp when the notification was created.</param>
public record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string Body,
    Guid? ReferenceId,
    string? ReferenceType,
    bool IsRead,
    DateTimeOffset CreatedAt
);

/// <summary>The count of unread notifications for the authenticated user.</summary>
/// <param name="Count">Number of unread notifications.</param>
public record UnreadCountResponse(int Count);

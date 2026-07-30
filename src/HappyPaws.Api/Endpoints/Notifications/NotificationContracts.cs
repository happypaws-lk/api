namespace HappyPaws.Api.Endpoints.Notifications;

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

public record UnreadCountResponse(int Count);

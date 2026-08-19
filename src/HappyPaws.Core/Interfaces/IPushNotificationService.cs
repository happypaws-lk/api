namespace HappyPaws.Core.Interfaces;

/// <summary>
/// Sends push notifications to user devices. Implementations use FCM in production and log to the console in development.
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Sends a push notification to all registered devices for a single user.
    /// </summary>
    Task SendToUserAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a push notification to all registered devices for multiple users in one batch.
    /// </summary>
    Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);
}

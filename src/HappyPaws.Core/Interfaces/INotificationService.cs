namespace HappyPaws.Core.Interfaces;

/// <summary>
/// Persists in-app notifications and dispatches push notifications to users' devices.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Saves a notification for one user and attempts a push notification.
    /// </summary>
    Task SendNotificationAsync(
        Guid userId,
        string type,
        string title,
        string body,
        Guid? referenceId = null,
        string? referenceType = null,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves notifications for multiple users and attempts a batch push notification.
    /// </summary>
    Task SendNotificationsAsync(
        IEnumerable<Guid> userIds,
        string type,
        string title,
        string body,
        Guid? referenceId = null,
        string? referenceType = null,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}

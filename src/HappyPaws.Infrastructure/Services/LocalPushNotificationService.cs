using HappyPaws.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Services;

/// <summary>
/// Development stub for push notifications. Writes notification details to the log instead of calling FCM.
/// </summary>
public sealed class LocalPushNotificationService : IPushNotificationService
{
    private readonly ILogger<LocalPushNotificationService> _logger;

    public LocalPushNotificationService(ILogger<LocalPushNotificationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs the notification to the console and returns immediately.
    /// </summary>
    public Task SendToUserAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV PUSH] To user {UserId}: {Title} — {Body}", userId, title, body);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs the notification with all recipient IDs to the console and returns immediately.
    /// </summary>
    public Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        var ids = string.Join(", ", userIds);
        _logger.LogInformation("[DEV PUSH] To users [{UserIds}]: {Title} — {Body}", ids, title, body);
        return Task.CompletedTask;
    }
}

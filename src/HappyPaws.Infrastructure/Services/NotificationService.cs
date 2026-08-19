using HappyPaws.Core.Entities;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Services;

/// <summary>
/// Saves in-app notifications to the database and dispatches a push notification via <see cref="IPushNotificationService"/>.
/// Push failures are logged but do not roll back the database write.
/// </summary>
public sealed class NotificationService(
    HappyPawsDbContext dbContext,
    IPushNotificationService pushNotificationService,
    ILogger<NotificationService> logger) : INotificationService
{
    /// <summary>
    /// Persists a notification for a single user, then attempts to send a push notification.
    /// </summary>
    public async Task SendNotificationAsync(
        Guid userId,
        string type,
        string title,
        string body,
        Guid? referenceId = null,
        string? referenceType = null,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Body = body,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                IsRead = false
            };

            dbContext.Set<Notification>().Add(notification);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await pushNotificationService.SendToUserAsync(userId, title, body, data, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send push notification to user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process notification for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Bulk-inserts notifications for multiple users and sends a batch push notification.
    /// Skips the entire operation if <paramref name="userIds"/> is empty.
    /// </summary>
    public async Task SendNotificationsAsync(
        IEnumerable<Guid> userIds,
        string type,
        string title,
        string body,
        Guid? referenceId = null,
        string? referenceType = null,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var userIdList = userIds.Distinct().ToList();
        if (userIdList.Count == 0) return;

        try
        {
            var notifications = userIdList.Select(uid => new Notification
            {
                UserId = uid,
                Type = type,
                Title = title,
                Body = body,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                IsRead = false
            }).ToList();

            dbContext.Set<Notification>().AddRange(notifications);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await pushNotificationService.SendToUsersAsync(userIdList, title, body, data, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send batch push notifications");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process batch notifications");
            throw;
        }
    }
}

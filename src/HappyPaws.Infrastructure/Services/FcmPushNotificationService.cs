using FirebaseAdmin.Messaging;
using HappyPaws.Core.Entities;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Services;

public sealed class FcmPushNotificationService(
    HappyPawsDbContext dbContext,
    ILogger<FcmPushNotificationService> logger) : IPushNotificationService
{
    public async Task SendToUserAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        var devices = await dbContext.Set<UserDevice>()
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (devices.Count == 0)
            return;

        await SendToDevicesAsync(devices, title, body, data, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        var devices = await dbContext.Set<UserDevice>()
            .AsNoTracking()
            .Where(d => userIds.Contains(d.UserId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (devices.Count == 0)
            return;

        await SendToDevicesAsync(devices, title, body, data, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendToDevicesAsync(List<UserDevice> devices, string title, string body, Dictionary<string, string>? data, CancellationToken cancellationToken)
    {
        var tokens = devices.Select(d => d.FcmToken).ToList();

#pragma warning disable CS0618
        var message = new MulticastMessage
        {
            Tokens = tokens,
            Notification = new FirebaseAdmin.Messaging.Notification
#pragma warning restore CS0618
            {
                Title = title,
                Body = body
            },
            Data = data ?? new Dictionary<string, string>()
        };

        try
        {
            var response = await FirebaseMessaging.DefaultInstance
                .SendEachForMulticastAsync(message, cancellationToken)
                .ConfigureAwait(false);

            if (response.FailureCount > 0)
            {
                var failedTokens = new List<string>();
                for (var i = 0; i < response.Responses.Count; i++)
                {
                    if (!response.Responses[i].IsSuccess)
                    {
                        var exception = response.Responses[i].Exception;
                        if (exception.MessagingErrorCode == MessagingErrorCode.Unregistered ||
                            exception.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
                        {
                            failedTokens.Add(tokens[i]);
                        }
                    }
                }

                if (failedTokens.Count > 0)
                {
                    var devicesToRemove = devices.Where(d => failedTokens.Contains(d.FcmToken)).ToList();

                    // Re-query tracked entities for deletion since devices were loaded AsNoTracking
                    var trackedDevices = await dbContext.Set<UserDevice>()
                        .Where(d => failedTokens.Contains(d.FcmToken))
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false);

                    dbContext.Set<UserDevice>().RemoveRange(trackedDevices);
                    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    logger.LogWarning("Removed {Count} stale FCM tokens", failedTokens.Count);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send FCM multicast message");
            throw;
        }
    }
}

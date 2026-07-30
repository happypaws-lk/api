namespace HappyPaws.Core.Interfaces;

public interface IPushNotificationService
{
    Task SendToUserAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);
    Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);
}

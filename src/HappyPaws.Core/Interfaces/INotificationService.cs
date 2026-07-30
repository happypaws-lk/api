namespace HappyPaws.Core.Interfaces;

public interface INotificationService
{
    Task SendNotificationAsync(
        Guid userId, 
        string type, 
        string title, 
        string body, 
        Guid? referenceId = null, 
        string? referenceType = null, 
        Dictionary<string, string>? data = null, 
        CancellationToken cancellationToken = default);

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

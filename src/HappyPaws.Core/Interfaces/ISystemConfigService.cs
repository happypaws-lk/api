namespace HappyPaws.Core.Interfaces;

/// <summary>
/// Reads operator-configurable system settings, such as the alert radius for rescue notifications.
/// </summary>
public interface ISystemConfigService
{
    /// <summary>
    /// Returns the radius in kilometres within which users should be alerted about new rescue cases.
    /// </summary>
    Task<int> GetAlertRadiusKmAsync(CancellationToken cancellationToken = default);
}

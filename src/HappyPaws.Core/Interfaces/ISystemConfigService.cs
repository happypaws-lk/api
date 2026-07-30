namespace HappyPaws.Core.Interfaces;

public interface ISystemConfigService
{
    Task<int> GetAlertRadiusKmAsync(CancellationToken cancellationToken = default);
}

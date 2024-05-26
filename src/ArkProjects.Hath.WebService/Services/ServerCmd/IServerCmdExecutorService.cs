namespace ArkProjects.Hath.WebService.Services;

public interface IServerCmdExecutorService
{
    Task<IResult> StillAliveAsync(CancellationToken ct = default);
    Task<IResult> ThreadedProxyTestAsync(string additional, CancellationToken ct = default);
    Task<IResult> SpeedTestAsync(string additional, CancellationToken ct = default);
    Task<IResult> RefreshSettingsAsync(CancellationToken ct = default);
    Task<IResult> StartDownloaderAsync(CancellationToken ct = default);
    Task<IResult> RefreshCertsAsync(CancellationToken ct = default);
}
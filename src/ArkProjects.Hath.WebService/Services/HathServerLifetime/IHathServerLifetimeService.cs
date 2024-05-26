namespace ArkProjects.Hath.WebService.Services;

public interface IHathServerLifetimeService
{
    HathServerStatus ServerStatus { get; }
    DateTimeOffset? LastStart { get; }
    DateTimeOffset? LastProlongSession { get; }

    Task NotifyStillAlive(CancellationToken ct = default);
    Task FetchCertificateAsync(CancellationToken ct = default);
    Task LoginAsync(CancellationToken ct = default);
    Task FetchRemoteStatAsync(CancellationToken ct = default);
    Task NotifySuspendAsync(CancellationToken ct = default);
    Task NotifyResumeAsync(CancellationToken ct = default);
    Task NotifyStartAsync(CancellationToken ct = default);
    Task NotifyStopAsync(CancellationToken ct = default);
    Task RefreshRemoteSettingsAsync(CancellationToken ct = default);
}
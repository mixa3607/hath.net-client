using ArkProjects.Hath.WebService.Quartz.Jobs;
using Quartz;

namespace ArkProjects.Hath.WebService.Services;

public class HathServerLifetimeWorker : IHostedLifecycleService
{
    private readonly ILogger<HathServerLifetimeWorker> _logger;
    private readonly IHathServerLifetimeService _hathServerLifetime;
    private readonly ISchedulerFactory _schedulerFactory;

    public HathServerLifetimeWorker(
        ILogger<HathServerLifetimeWorker> logger, IHathServerLifetimeService hathServerLifetime, ISchedulerFactory schedulerFactory)
    {
        _logger = logger;
        _hathServerLifetime = hathServerLifetime;
        _schedulerFactory = schedulerFactory;
    }

    public async Task StartingAsync(CancellationToken ct)
    {
        await _hathServerLifetime.FetchRemoteStatAsync(ct);
        await _hathServerLifetime.FetchCertificateAsync(ct);
        await _hathServerLifetime.LoginAsync(ct);
    }

    public async Task StoppingAsync(CancellationToken ct)
    {
        await _hathServerLifetime.NotifyStopAsync(ct);
    }

    public async Task StartedAsync(CancellationToken ct)
    {
        await _hathServerLifetime.NotifyStartAsync(ct);
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler(ct);
            await scheduler.TriggerJob(new JobKey(nameof(DownloadPendingGalleriesJob)), ct);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
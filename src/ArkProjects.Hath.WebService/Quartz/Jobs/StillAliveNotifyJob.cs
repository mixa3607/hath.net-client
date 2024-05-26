using ArkProjects.Hath.WebService.Services;
using Quartz;

namespace ArkProjects.Hath.WebService.Quartz.Jobs;

public class StillAliveNotifyJob : IJob
{
    private readonly ILogger<StillAliveNotifyJob> _logger;
    private readonly IHathServerLifetimeService _hathServerLifetime;

    public StillAliveNotifyJob(ILogger<StillAliveNotifyJob> logger, IHathServerLifetimeService hathServerLifetime)
    {
        _logger = logger;
        _hathServerLifetime = hathServerLifetime;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        await _hathServerLifetime.NotifyStillAlive(ct);
    }
}
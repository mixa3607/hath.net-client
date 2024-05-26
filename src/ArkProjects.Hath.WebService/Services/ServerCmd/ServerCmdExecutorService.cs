using ArkProjects.Hath.WebService.Misc;
using ArkProjects.Hath.WebService.Quartz.Jobs;
using Quartz;

namespace ArkProjects.Hath.WebService.Services;

public class ServerCmdExecutorService : IServerCmdExecutorService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IHathServerLifetimeService _hathServerLifetime;
    private IScheduler? _scheduler;

    public ServerCmdExecutorService(ISchedulerFactory schedulerFactory, IHathServerLifetimeService hathServerLifetime)
    {
        _schedulerFactory = schedulerFactory;
        _hathServerLifetime = hathServerLifetime;
    }

    public Task<IResult> StillAliveAsync(CancellationToken ct = default)
    {
        var result = new HathTextResult("I feel FANTASTIC and I'm still alive", StatusCodes.Status200OK);
        return Task.FromResult<IResult>(result);
    }

    public async Task<IResult> ThreadedProxyTestAsync(string additional, CancellationToken ct = default)
    {
        var args = ThreadedProxyTestServerCmdArgs.Parse(additional);
        var fuckingMagic = Random.Shared.Next() * int.MaxValue;
        var testUrl =
            $"{args.Protocol}://{args.HostName}:{args.Port}" +
            $"/t/{args.TestSize}/{args.TestTime}/{args.TestKey}/{fuckingMagic}";

        var tasks = new List<Task<TestCommandResult>>();
        var combinedCts =
            CancellationTokenSource.CreateLinkedTokenSource(ct, new CancellationTokenSource(60_000).Token);
        for (int i = 0; i < args.TestCount; i++)
        {
            var task = TestRequestSender.MakeTestRequestAsync(testUrl, args.TestSize, combinedCts.Token);
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);
        var successCompleted = results.Where(x => x.Success).ToArray();

        var text = $"OK:{successCompleted.Length}-{(long)successCompleted.Sum(x => x.Elapsed.TotalMilliseconds)}";
        return new HathTextResult(text, StatusCodes.Status200OK);
    }

    public async Task<IResult> SpeedTestAsync(string additional, CancellationToken ct = default)
    {
        var args = SpeedTestServerCmdArgs.Parse(additional);
        var testSize = args.TestSize ?? 1000000;
        return new HathRandomBlobResult(testSize, StatusCodes.Status200OK);
    }

    public async Task<IResult> RefreshSettingsAsync(CancellationToken ct = default)
    {
        await _hathServerLifetime.RefreshRemoteSettingsAsync(ct);
        return Results.Ok();
    }

    public async Task<IResult> StartDownloaderAsync(CancellationToken ct = default)
    {
        _scheduler ??= await _schedulerFactory.GetScheduler(ct);
        await _scheduler.TriggerJob(new JobKey(nameof(DownloadPendingGalleriesJob)), ct);
        return Results.Ok();
    }

    public async Task<IResult> RefreshCertsAsync(CancellationToken ct = default)
    {
        await _hathServerLifetime.FetchCertificateAsync(ct);
        return Results.Ok();
    }
}
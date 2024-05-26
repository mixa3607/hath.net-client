using ArkProjects.Hath.ClientApi;
using ArkProjects.Hath.WebService.Services;

namespace ArkProjects.Hath.WebService.HathApi;

public class HathOverloadDetector
{
    private readonly ILogger<HathOverloadDetector> _logger;
    private DateTimeOffset _lastOverloadNotify = DateTimeOffset.MinValue;

    public int ActiveRequests { get; private set; }
    public int OverloadNotifies { get; private set; }
    public int MaxConnections => _settingsStorage.GetMaxConcurrentRequest();

    private readonly ISettingsStorage _settingsStorage;
    private readonly HathClient _client;
    private readonly TimeSpan _overloadNotifyPeriod = TimeSpan.FromMinutes(5);
    private readonly IStatisticsManager _statistics;

    public HathOverloadDetector(ISettingsStorage settingsStorage, HathClient client,
        ILogger<HathOverloadDetector> logger, IStatisticsManager statistics)
    {
        _settingsStorage = settingsStorage;
        _client = client;
        _logger = logger;
        _statistics = statistics;
    }

    public async Task InvokeAsync(RequestDelegate next, HttpContext context)
    {
        lock (this)
        {
            ActiveRequests++;
            _ = _statistics.ConnectionsOpenAsync(ActiveRequests);
            var now = DateTimeOffset.UtcNow;
            if (ActiveRequests > MaxConnections * .8 && now - _lastOverloadNotify > _overloadNotifyPeriod)
            {
                _lastOverloadNotify = now;
                _ = NotifyOverloadAsync();
            }
        }

        try
        {
            await next(context);
        }
        finally
        {
            lock (this)
            {
                ActiveRequests--;
            }
        }
    }

    private async Task NotifyOverloadAsync()
    {
        var maxAttempts = 5;
        for (int currAttempt = 0; currAttempt < maxAttempts; currAttempt++)
        {
            try
            {
                var resp = await _client.OverloadAsync(CancellationToken.None);
                _logger.LogInformation("Control server notified about overload successfully");
                OverloadNotifies++;
                return;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Control server reported error on overload {message}",
                    (e as HathClientException)?.Response.RawText);
            }
        }

        _logger.LogError("Max attempts to notify control server about overload reached");
    }
}
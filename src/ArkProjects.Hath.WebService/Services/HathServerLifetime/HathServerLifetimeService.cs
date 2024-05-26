using ArkProjects.Hath.ClientApi;

namespace ArkProjects.Hath.WebService.Services;

public class HathServerLifetimeService : IHathServerLifetimeService
{
    private readonly HathClient _client;
    private readonly ISettingsStorage _settingsStorage;
    private readonly ILogger<HathServerLifetimeService> _logger;
    private readonly SemaphoreSlim _changeStatusSemaphore = new(1, 1);

    public HathServerStatus ServerStatus { get; private set; }
    public DateTimeOffset? LastStart { get; private set; }
    public DateTimeOffset? LastProlongSession { get; private set; }

    public HathServerLifetimeService(HathClient client, ISettingsStorage settingsStorage,
        ILogger<HathServerLifetimeService> logger)
    {
        _client = client;
        _settingsStorage = settingsStorage;
        _logger = logger;
    }

    public async Task NotifyStillAlive(CancellationToken ct = default)
    {
        await _changeStatusSemaphore.WaitAsync(ct);
        try
        {
            var resp = await _client.StillAliveAsync(false, ct);
            LastProlongSession = DateTimeOffset.UtcNow;
            _logger.LogInformation("Server still alive notified");
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Control server reported error on still alive {message}",
                (e as HathClientException)?.Response.RawText);
            throw;
        }
        finally
        {
            _changeStatusSemaphore.Release();
        }
    }

    public async Task FetchCertificateAsync(CancellationToken ct = default)
    {
        await _changeStatusSemaphore.WaitAsync(ct);
        try
        {
            var resp = await _client.GetCertificateAsync(ct);
            _settingsStorage.UpdateCert(resp.CertBytes!);
            _logger.LogInformation("Server cert received. {cert}",
                _settingsStorage.Settings.Certificate!.SubjectName.Name);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Control server reported error on fetch cert {message}",
                (e as HathClientException)?.Response.RawText);
            throw;
        }
        finally
        {
            _changeStatusSemaphore.Release();
        }
    }

    public async Task RefreshRemoteSettingsAsync(CancellationToken ct = default)
    {
        await _changeStatusSemaphore.WaitAsync(ct);
        try
        {
            var resp = await _client.ClientSettingsAsync(ct);
            _settingsStorage.Update(resp.Properties);
            _logger.LogInformation("Server settings received");
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Control server reported error on refresh settings {message}",
                (e as HathClientException)?.Response.RawText);
            throw;
        }
        finally
        {
            _changeStatusSemaphore.Release();
        }
    }

    public async Task LoginAsync(CancellationToken ct = default)
    {
        await _changeStatusSemaphore.WaitAsync(ct);
        try
        {
            var resp = await _client.ClientLoginAsync(ct);
            _settingsStorage.Update(resp.Properties);
            _logger.LogInformation("Server login received");
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Control server reported error on login {message}",
                (e as HathClientException)?.Response.RawText);
            throw;
        }
        finally
        {
            _changeStatusSemaphore.Release();
        }
    }

    public async Task FetchRemoteStatAsync(CancellationToken ct = default)
    {
        await _changeStatusSemaphore.WaitAsync(ct);
        try
        {
            var resp = await _client.ServerStatAsync(ct);
            _settingsStorage.Update(resp.Properties);
            if (_settingsStorage.Settings.LatestClientBuild != _settingsStorage.Settings.CurrentClientBuild)
            {
                _logger.LogError("Server reported {act} latest client version but implementation is {curr}",
                    _settingsStorage.Settings.LatestClientBuild, _settingsStorage.Settings.CurrentClientBuild);
            }

            if (_settingsStorage.Settings.CurrentClientBuild < _settingsStorage.Settings.MinimalClientBuild)
            {
                _logger.LogCritical("Server reported {act} minimal client version but implementation is {curr}",
                    _settingsStorage.Settings.MinimalClientBuild, _settingsStorage.Settings.CurrentClientBuild);
                throw new Exception("Version outdated");
            }

            _logger.LogInformation("Server stats received");
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Control server reported error on server stat {message}",
                (e as HathClientException)?.Response.RawText);
            throw;
        }
        finally
        {
            _changeStatusSemaphore.Release();
        }
    }

    public async Task NotifySuspendAsync(CancellationToken ct = default)
    {
        await _changeStatusSemaphore.WaitAsync(ct);
        var prevStatus = ServerStatus;
        try
        {
            var resp = await _client.ClientSuspendAsync(ct);
            _logger.LogInformation("Control server notified about suspend successfully");
            ServerStatus = HathServerStatus.Suspended;
        }
        catch (Exception e)
        {
            ServerStatus = prevStatus;
            _logger.LogError(e, "Control server reported error on suspend {message}",
                (e as HathClientException)?.Response.RawText);
            throw;
        }
        finally
        {
            _changeStatusSemaphore.Release();
        }
    }

    public async Task NotifyResumeAsync(CancellationToken ct = default)
    {
        await _changeStatusSemaphore.WaitAsync(ct);
        var prevStatus = ServerStatus;
        try
        {
            var resp = await _client.ClientResumeAsync(ct);
            _logger.LogInformation("Control server notified about resume successfully");
            ServerStatus = HathServerStatus.Running;
            LastStart = DateTimeOffset.UtcNow;
            LastProlongSession = DateTimeOffset.UtcNow;
        }
        catch (Exception e)
        {
            ServerStatus = prevStatus;
            _logger.LogError(e, "Control server reported error on resume {message}",
                (e as HathClientException)?.Response.RawText);
            throw;
        }
        finally
        {
            _changeStatusSemaphore.Release();
        }
    }

    public async Task NotifyStartAsync(CancellationToken ct = default)
    {
        if (_settingsStorage.Settings.NoStartNotify)
        {
            ServerStatus = HathServerStatus.Running;
            LastStart = DateTimeOffset.UtcNow;
            return;
        }

        await _changeStatusSemaphore.WaitAsync(ct);
        try
        {
            var maxAttempts = 5;
            for (int currAttempt = 0; currAttempt < maxAttempts; currAttempt++)
            {
                try
                {
                    var resp = await _client.ClientStartAsync(ct);
                    _logger.LogInformation("Control server notified about start successfully");
                    ServerStatus = HathServerStatus.Running;
                    LastStart = DateTimeOffset.UtcNow;
                    LastProlongSession = DateTimeOffset.UtcNow;
                    return;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Control server reported error on start {message}",
                        (e as HathClientException)?.Response.RawText);
                }
            }

            throw new Exception("Max attempts to notify control server about start reached");
        }
        finally
        {
            _changeStatusSemaphore.Release();
        }
    }

    public async Task NotifyStopAsync(CancellationToken ct = default)
    {
        if (_settingsStorage.Settings.NoStopNotify)
        {
            ServerStatus = HathServerStatus.Stopped;
            return;
        }

        await _changeStatusSemaphore.WaitAsync(ct);
        var prevStatus = ServerStatus;
        try
        {
            var maxAttempts = 5;
            for (int currAttempt = 0; currAttempt < maxAttempts; currAttempt++)
            {
                try
                {
                    var resp = await _client.ClientStopAsync(ct);
                    _logger.LogInformation("Control server notified about stop successfully");
                    ServerStatus = HathServerStatus.Stopped;
                    return;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Control server reported error on stop {message}",
                        (e as HathClientException)?.Response.RawText);
                }
            }

            ServerStatus = prevStatus;
            throw new Exception("Max attempts to notify control server about stop reached");
        }
        finally
        {
            _changeStatusSemaphore.Release();
        }
    }
}